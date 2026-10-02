using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Laboratory;

/// <summary>"ส่งแล็บ", "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ" and "งานค้างของหมอ · ผลที่ยังไม่มีคนอ่าน".</summary>
[Collection(nameof(ApiCollection))]
public class LabRequestTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] LabScopes =
        [Scopes.Lab.Read, Scopes.Lab.Request, Scopes.Lab.Write, Scopes.Record.Read, .. ClinicalScopes];

    private async Task<(OpenCase Open, HttpClient Vet, string LabId)> RequestedAsync()
    {
        var open = await OpenCaseAsync(fixture, Token);
        var vet = fixture.Vet(open.Clinic, LabScopes);

        var response = await vet.PostAsJsonAsync(
            "/staff/lab-requests",
            new
            {
                pet_id = open.Clinic.PetId,
                consultation_id = open.ConsultationId,
                destination = "in_house",
                tests = new[] { new { name = "CBC" }, new { name = "Chemistry" } },
            },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        // The branch comes from the visit, not from the caller.
        Assert.Equal(open.Clinic.BranchId, body.GetProperty("branch_id").GetString());

        return (open, vet, body.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task A_new_request_is_pending_on_the_worklist()
    {
        var (_, vet, labId) = await RequestedAsync();
        using var client = vet;

        var pending = await client.GetFromJsonAsync<JsonElement>("/staff/lab-requests?status=pending&requested_by=me", Token);

        Assert.Contains(pending.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == labId);
        Assert.StartsWith("lab_", labId);
    }

    [Fact]
    public async Task Results_are_flagged_against_their_range_and_wait_to_be_read()
    {
        var (_, vet, labId) = await RequestedAsync();
        using var __ = vet;

        var response = await vet.PostAsJsonAsync(
            $"/staff/lab-requests/{labId}/results",
            new
            {
                results = new object[]
                {
                    new { analyte = "ALT", value = 180m, unit = "U/L", reference_low = 10m, reference_high = 100m },
                    new { analyte = "HCT", value = 22m, unit = "%", reference_low = 37m, reference_high = 55m },
                    new { analyte = "Glucose", value = 95m, unit = "mg/dL", reference_low = 70m, reference_high = 140m },
                    new { analyte = "Heartworm Ag", value_text = "positive", abnormal = true },
                },
            },
            Token);

        response.EnsureSuccessStatusCode();
        var request = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("resulted", request.GetProperty("status").GetString());
        Assert.Equal(3, request.GetProperty("abnormal_count").GetInt32());

        var flags = request.GetProperty("results").EnumerateArray()
            .ToDictionary(item => item.GetProperty("analyte").GetString()!, item => item.GetProperty("flag").GetString());

        Assert.Equal("high", flags["ALT"]);
        Assert.Equal("low", flags["HCT"]);
        Assert.Equal("normal", flags["Glucose"]);
        Assert.Equal("abnormal", flags["Heartworm Ag"]);
    }

    [Fact]
    public async Task Reading_the_results_takes_them_off_the_unread_list_and_new_results_put_them_back()
    {
        var (_, vet, labId) = await RequestedAsync();
        using var __ = vet;

        object Result(string analyte) => new { results = new[] { new { analyte, value = 1m } } };

        (await vet.PostAsJsonAsync($"/staff/lab-requests/{labId}/results", Result("A"), Token)).EnsureSuccessStatusCode();

        var reviewed = await (await vet.PostAsync($"/staff/lab-requests/{labId}/review", null, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("reviewed", reviewed.GetProperty("status").GetString());

        (await vet.PostAsJsonAsync($"/staff/lab-requests/{labId}/results", Result("B"), Token)).EnsureSuccessStatusCode();

        var unread = await vet.GetFromJsonAsync<JsonElement>("/staff/lab-requests?status=resulted&requested_by=me", Token);
        Assert.Contains(unread.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == labId);
    }

    [Fact]
    public async Task Reviewing_a_request_with_no_results_is_refused()
    {
        var (_, vet, labId) = await RequestedAsync();
        using var __ = vet;

        var response = await vet.PostAsync($"/staff/lab-requests/{labId}/review", null, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Entering_results_needs_lab_write()
    {
        var (open, vet, labId) = await RequestedAsync();
        using var __ = vet;
        using var requester = fixture.Vet(open.Clinic, Scopes.Lab.Request, Scopes.Lab.Read);

        var response = await requester.PostAsJsonAsync(
            $"/staff/lab-requests/{labId}/results", new { results = new[] { new { analyte = "X", value = 1m } } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_request_at_another_branch_is_not_found()
    {
        var (_, vet, labId) = await RequestedAsync();
        using var __ = vet;
        using var elsewhere = fixture.Authenticated(TestTokens.Staff(fixture, scopes: LabScopes, branches: ["brn_elsewhere"]));

        var response = await elsewhere.PostAsync($"/staff/lab-requests/{labId}/review", null, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_external_request_must_name_the_lab()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, LabScopes);

        var response = await vet.PostAsJsonAsync(
            "/staff/lab-requests",
            new { pet_id = clinic.PetId, branch_id = clinic.BranchId, destination = "external", tests = new[] { new { name = "Histopathology" } } },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid_lab_request", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());
    }
}
