using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Auditing;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Api.Laboratory.LabScenario;

namespace HappyPaws.IntegrationTests.Api.Laboratory;

/// <summary>
/// <c>docs/contract-extensions.md § 53</c>: the vet may leave a note when reading results. It is
/// kept as <c>review_note</c>, listed with the request, audited, and cleared with the rest of the
/// reading when new results put the request back in front of a vet.
/// </summary>
[Collection(nameof(ApiCollection))]
public class LabReviewNoteTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private const string Note = "ค่าตับสูง ให้ยาบำรุงตับ นัดตรวจซ้ำ 2 สัปดาห์";

    [Fact]
    public async Task The_note_is_kept_listed_and_audited()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        await ResultedAsync(lab, labId, Token);

        var response = await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = $"  {Note}  " }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reviewed = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("reviewed", reviewed.GetProperty("status").GetString());
        Assert.Equal(Note, reviewed.GetProperty("review_note").GetString());

        var listed = await lab.GetFromJsonAsync<JsonElement>("/staff/lab-requests?status=reviewed", Token);
        var item = listed.GetProperty("data").EnumerateArray().Single(entry => entry.GetProperty("id").GetString() == labId);
        Assert.Equal(Note, item.GetProperty("review_note").GetString());

        var entry = Assert.Single(await AuditAsync(fixture, AuditActions.LabResultsReviewed, labId, Token));
        using var details = JsonDocument.Parse(entry.DetailsJson!);
        Assert.Equal(Note, details.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task Reading_with_no_body_or_a_blank_note_leaves_no_note()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        await ResultedAsync(lab, labId, Token);

        var reviewed = await (await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = "   " }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("reviewed", reviewed.GetProperty("status").GetString());
        Assert.False(reviewed.TryGetProperty("review_note", out _));
    }

    [Fact]
    public async Task A_note_over_1000_characters_is_refused()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        await ResultedAsync(lab, labId, Token);

        var response = await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = new string('ก', 1001) }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("note", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Reading_again_does_not_replace_the_note()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        await ResultedAsync(lab, labId, Token);

        await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = Note }, Token);
        var again = await (await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = "อีกครั้ง" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(Note, again.GetProperty("review_note").GetString());
        Assert.Single(await AuditAsync(fixture, AuditActions.LabResultsReviewed, labId, Token));
    }

    [Fact]
    public async Task New_results_clear_the_note_with_the_reading()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        await ResultedAsync(lab, labId, Token);
        await lab.PostAsJsonAsync($"/staff/lab-requests/{labId}/review", new { note = Note }, Token);

        var more = await (await lab.PostAsJsonAsync(
                $"/staff/lab-requests/{labId}/results", new { results = new[] { new { analyte = "BUN", value = 20m } } }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("resulted", more.GetProperty("status").GetString());
        Assert.False(more.TryGetProperty("review_note", out _));
    }
}
