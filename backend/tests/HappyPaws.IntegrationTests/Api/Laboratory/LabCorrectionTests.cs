using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Api.Laboratory.LabScenario;

namespace HappyPaws.IntegrationTests.Api.Laboratory;

/// <summary>
/// <c>docs/contract-extensions.md § 52</c>: one value is corrected or struck out with a reason, the
/// value it replaced stays on file, the flags and the abnormal count follow, and results a vet had
/// read go back to "รออ่านผล".
/// </summary>
[Collection(nameof(ApiCollection))]
public class LabCorrectionTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static Task<HttpResponseMessage> AmendAsync(HttpClient client, string labId, string resultId, object body, uint? ifMatch = null) =>
        SendAsync(client, $"/staff/lab-requests/{labId}/results/{resultId}/amend", body, ifMatch);

    private static Task<HttpResponseMessage> VoidAsync(HttpClient client, string labId, string resultId, object body, uint? ifMatch = null) =>
        SendAsync(client, $"/staff/lab-requests/{labId}/results/{resultId}/void", body, ifMatch);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string path, object body, uint? ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return client.SendAsync(request, Token);
    }

    private static readonly object CorrectedAlt =
        new { reason = "พิมพ์ผิด ค่าจริง 80", value = 80m, unit = "U/L", reference_low = 10m, reference_high = 100m };

    [Fact]
    public async Task A_correction_keeps_the_value_it_replaced_and_flags_the_new_one()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        Assert.Equal(2, resulted.GetProperty("abnormal_count").GetInt32());

        var amended = await OkAsync(await AmendAsync(lab, labId, ResultId(resulted, "ALT"), CorrectedAlt));

        var alt = Result(amended, "ALT");
        Assert.Equal(80m, alt.GetProperty("value_numeric").GetDecimal());
        Assert.Equal("normal", alt.GetProperty("flag").GetString());
        Assert.True(alt.GetProperty("amended").GetBoolean());
        Assert.Equal("พิมพ์ผิด ค่าจริง 80", alt.GetProperty("amend_reason").GetString());
        Assert.False(alt.GetProperty("voided").GetBoolean());

        var previous = Assert.Single(alt.GetProperty("previous_values").EnumerateArray());
        Assert.Equal(180m, previous.GetProperty("value_numeric").GetDecimal());
        Assert.Equal("high", previous.GetProperty("flag").GetString());
        Assert.Equal("พิมพ์ผิด ค่าจริง 80", previous.GetProperty("reason").GetString());

        Assert.Equal(1, amended.GetProperty("abnormal_count").GetInt32());

        // Still waiting on a vet: it had not been read.
        Assert.Equal("resulted", amended.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_correction_is_audited_with_the_value_before_and_after_and_why()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);

        await OkAsync(await AmendAsync(lab, labId, ResultId(resulted, "ALT"), CorrectedAlt));

        var entry = Assert.Single(await AuditAsync(fixture, AuditActions.LabResultAmended, labId, Token));
        using var details = JsonDocument.Parse(entry.DetailsJson!);
        var root = details.RootElement;

        Assert.Equal(180m, root.GetProperty("before").GetProperty("value_numeric").GetDecimal());
        Assert.Equal(80m, root.GetProperty("after").GetProperty("value_numeric").GetDecimal());
        Assert.Equal("normal", root.GetProperty("after").GetProperty("flag").GetString());
        Assert.Equal("พิมพ์ผิด ค่าจริง 80", root.GetProperty("reason").GetString());
        Assert.False(root.GetProperty("reopened").GetBoolean());
    }

    [Fact]
    public async Task Correcting_results_a_vet_has_read_puts_them_back_in_front_of_a_vet()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);

        var reviewed = await OkAsync(await lab.PostAsJsonAsync(
            $"/staff/lab-requests/{labId}/review", new { note = "ตับสูง ตรวจซ้ำ" }, Token));
        Assert.Equal("reviewed", reviewed.GetProperty("status").GetString());

        var amended = await OkAsync(await AmendAsync(lab, labId, ResultId(resulted, "ALT"), CorrectedAlt));

        Assert.Equal("resulted", amended.GetProperty("status").GetString());
        Assert.False(amended.TryGetProperty("reviewed_at", out _));
        Assert.False(amended.TryGetProperty("review_note", out _));

        var unread = await lab.GetFromJsonAsync<JsonElement>("/staff/lab-requests?status=resulted", Token);
        Assert.Contains(unread.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == labId);

        var entry = Assert.Single(await AuditAsync(fixture, AuditActions.LabResultAmended, labId, Token));
        using var details = JsonDocument.Parse(entry.DetailsJson!);
        Assert.True(details.RootElement.GetProperty("reopened").GetBoolean());
    }

    [Fact]
    public async Task A_voided_value_stays_listed_and_counts_towards_nothing()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        var hct = ResultId(resulted, "HCT");

        var voided = await OkAsync(await VoidAsync(lab, labId, hct, new { reason = "ตัวอย่างของสัตว์ตัวอื่น" }));

        var value = Result(voided, "HCT");
        Assert.True(value.GetProperty("voided").GetBoolean());
        Assert.Equal("ตัวอย่างของสัตว์ตัวอื่น", value.GetProperty("void_reason").GetString());
        Assert.Equal(22m, value.GetProperty("value_numeric").GetDecimal());
        Assert.Equal(1, voided.GetProperty("abnormal_count").GetInt32());

        var entry = Assert.Single(await AuditAsync(fixture, AuditActions.LabResultVoided, labId, Token));
        Assert.Contains("ตัวอย่างของสัตว์ตัวอื่น", entry.DetailsJson, StringComparison.Ordinal);

        // Once is the record; a voided value is not corrected, nor voided again.
        var again = await VoidAsync(lab, labId, hct, new { reason = "อีกครั้ง" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("state_conflict", await ErrorCodeAsync(again, Token));

        var amend = await AmendAsync(lab, labId, hct, new { reason = "แก้", value = 40m });
        Assert.Equal(HttpStatusCode.Conflict, amend.StatusCode);
        Assert.Equal("state_conflict", await ErrorCodeAsync(amend, Token));
    }

    [Fact]
    public async Task Voiding_a_value_a_vet_has_read_puts_the_results_back_in_front_of_a_vet()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        await OkAsync(await lab.PostAsync($"/staff/lab-requests/{labId}/review", null, Token));

        var voided = await OkAsync(await VoidAsync(lab, labId, ResultId(resulted, "HCT"), new { reason = "ผิดตัว" }));

        Assert.Equal("resulted", voided.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("amend")]
    [InlineData("void")]
    public async Task A_correction_needs_a_reason(string action)
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        var alt = ResultId(resulted, "ALT");

        var response = action == "amend"
            ? await AmendAsync(lab, labId, alt, new { value = 80m, reason = "  " })
            : await VoidAsync(lab, labId, alt, new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("reason", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_corrected_value_still_needs_a_value()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);

        var response = await AmendAsync(lab, labId, ResultId(resulted, "ALT"), new { reason = "ลืมใส่ค่า" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid_lab_result", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task Correcting_needs_lab_write()
    {
        var (clinic, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        using var vet = fixture.Vet(clinic, Scopes.Lab.Read, Scopes.Lab.Request);

        Assert.Equal(HttpStatusCode.Forbidden, (await AmendAsync(vet, labId, ResultId(resulted, "ALT"), CorrectedAlt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await VoidAsync(vet, labId, ResultId(resulted, "ALT"), new { reason = "x" })).StatusCode);
    }

    [Fact]
    public async Task A_request_at_another_branch_or_a_value_not_on_it_is_not_found()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        using var elsewhere = Elsewhere(fixture);

        Assert.Equal(HttpStatusCode.NotFound, (await AmendAsync(elsewhere, labId, ResultId(resulted, "ALT"), CorrectedAlt)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await VoidAsync(elsewhere, labId, ResultId(resulted, "ALT"), new { reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AmendAsync(lab, labId, "lbr_missing", CorrectedAlt)).StatusCode);
    }

    [Fact]
    public async Task A_correction_against_a_version_already_changed_is_a_conflict()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var resulted = await ResultedAsync(lab, labId, Token);
        var version = resulted.GetProperty("version").GetUInt32();

        var first = await AmendAsync(lab, labId, ResultId(resulted, "ALT"), CorrectedAlt, version);
        var amended = await OkAsync(first);
        Assert.NotEqual(version, amended.GetProperty("version").GetUInt32());
        Assert.Equal($"\"{amended.GetProperty("version").GetUInt32()}\"", first.Headers.ETag?.Tag);

        var stale = await VoidAsync(lab, labId, ResultId(resulted, "HCT"), new { reason = "ผิดตัว" }, version);

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", await ErrorCodeAsync(stale, Token));
    }
}
