using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Consultations;

/// <summary>
/// <c>GET /staff/consultations/{id}</c> — the one read in a section of eight writes, added so a
/// reloaded consulting-room page can show a draft left open. See
/// <c>docs/contract-extensions.md § 14</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ReadConsultationTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_reloaded_page_sees_everything_already_in_the_case()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        (await vet.PostAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}/vitals", new { weight_kg = 8.4 }, Token)).EnsureSuccessStatusCode();
        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var response = await vet.GetAsync($"/staff/consultations/{open.ConsultationId}", Token);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("draft", body.GetProperty("status").GetString());
        Assert.Equal(8.4m, body.GetProperty("vitals")[0].GetProperty("weight_kg").GetDecimal());
        Assert.Equal("ผิวหนังอักเสบ", body.GetProperty("diagnoses")[0].GetProperty("description").GetString());

        // The version a later PATCH sends back, as the ETag every write here also returns.
        Assert.Equal($"\"{body.GetProperty("version").GetUInt32()}\"", response.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task A_colleague_reading_the_case_is_in_the_access_log_and_its_vet_is_not()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        (await vet.GetAsync($"/staff/consultations/{open.ConsultationId}", Token)).EnsureSuccessStatusCode();

        using var colleague = fixture.Authenticated(TestTokens.Staff(
            fixture, "usr_colleague", "stf_colleague", ClinicalScopes, [open.Clinic.BranchId]));

        (await colleague.GetAsync($"/staff/consultations/{open.ConsultationId}", Token)).EnsureSuccessStatusCode();

        var log = await vet.GetFromJsonAsync<JsonElement>($"/staff/audit-logs/access?pet_id={open.Clinic.PetId}", Token);
        var views = log.GetProperty("data").EnumerateArray()
            .Where(entry => entry.GetProperty("action").GetString() == "consultation.viewed"
                && entry.GetProperty("resource_id").GetString() == open.ConsultationId)
            .ToList();

        // One row: the colleague's. The vet reloading their own draft is not news.
        Assert.Single(views);
    }

    [Fact]
    public async Task Reading_needs_consult_read_and_the_branch()
    {
        var open = await OpenCaseAsync(fixture, Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var assistant = fixture.Vet(open.Clinic, Scopes.Queue.Read, Scopes.Patient.Read, Scopes.Vitals.Write);
        using var outsider = fixture.Vet(elsewhere, Scopes.Consult.Read);

        var noScope = await assistant.GetAsync($"/staff/consultations/{open.ConsultationId}", Token);
        var otherBranch = await outsider.GetAsync($"/staff/consultations/{open.ConsultationId}", Token);
        var unknown = await fixture.Vet(open.Clinic).GetAsync("/staff/consultations/con_missing", Token);

        Assert.Equal(HttpStatusCode.Forbidden, noScope.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherBranch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
