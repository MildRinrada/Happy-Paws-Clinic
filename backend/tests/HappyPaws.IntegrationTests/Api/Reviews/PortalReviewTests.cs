using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Reviews;

/// <summary>
/// <c>docs/contract-extensions.md § 36</c>: a signed-in customer reviews their own visits of the
/// last 60 days, once each, and the review waits for staff.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PortalReviewTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    private async Task<List<string>> PendingIdsAsync(HttpClient portal) =>
        [.. (await OkAsync(await portal.GetAsync("/portal/reviews/pending", Token)))
            .GetProperty("data").EnumerateArray().Select(visit => visit.GetProperty("visit_id").GetString()!)];

    [Fact]
    public async Task The_pending_list_is_the_customers_own_finished_visits_of_the_last_sixty_days()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 3, Token);
        var closed = await ReviewScenario.ConsultationAsync(fixture, clinic, daysAgo: 10, closed: true, Token);
        var open = await ReviewScenario.ConsultationAsync(fixture, clinic, daysAgo: 12, closed: false, Token);
        var voided = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 14, Token, voided: true);
        var shopSale = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 16, Token, withPet: false);
        var tooOld = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 61, Token);
        var edge = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 60, Token);
        var notMine = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 3, Token, other.CustomerId, other.PetId);

        using var portal = fixture.Portal(clinic);
        var pending = await OkAsync(await portal.GetAsync("/portal/reviews/pending", Token));
        var ids = pending.GetProperty("data").EnumerateArray().Select(visit => visit.GetProperty("visit_id").GetString()).ToList();

        Assert.Equal([bill, closed, edge], ids);
        Assert.DoesNotContain(open, ids);
        Assert.DoesNotContain(voided, ids);
        Assert.DoesNotContain(shopSale, ids);
        Assert.DoesNotContain(tooOld, ids);
        Assert.DoesNotContain(notMine, ids);

        var first = pending.GetProperty("data")[0];
        Assert.Equal("ลูน่า", first.GetProperty("pet_name").GetString());
        Assert.Equal("สาขาทดสอบ", first.GetProperty("branch_name").GetString());
        Assert.Equal(ReviewScenario.Today.AddDays(-3).ToString("yyyy-MM-dd"), first.GetProperty("visit_on").GetString());
    }

    [Fact]
    public async Task A_bill_and_a_consultation_on_one_day_are_one_visit_named_by_the_consultation()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 5, Token, hour: 9);
        var consultation = await ReviewScenario.ConsultationAsync(fixture, clinic, daysAgo: 5, closed: true, Token, hour: 10);

        using var portal = fixture.Portal(clinic);
        Assert.Equal([consultation], await PendingIdsAsync(portal));

        // Either record names the same visit, so a review through the bill uses it up.
        await OkAsync(await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: bill), Token));
        Assert.Empty(await PendingIdsAsync(portal));
    }

    [Fact]
    public async Task A_review_is_pending_shows_the_first_name_and_goes_once_per_visit()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await ReviewScenario.RenameAsync(fixture, clinic.CustomerId, "คุณมายด์ สุขใจ", Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 2, Token);

        using var portal = fixture.Portal(clinic);
        Assert.Equal("มายด์", (await OkAsync(await portal.GetAsync("/portal/reviews/pending", Token))).GetProperty("first_name").GetString());

        var response = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(4, "  จองง่าย\r\nรอไม่นาน  ", visitId: bill), Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var review = await OkAsync(response);

        Assert.Equal("pending", review.GetProperty("status").GetString());
        Assert.Equal("มายด์", review.GetProperty("display_name").GetString());
        Assert.Equal("จองง่าย\nรอไม่นาน", review.GetProperty("text").GetString());
        Assert.Equal(4, review.GetProperty("rating").GetInt32());

        Assert.DoesNotContain(bill, await PendingIdsAsync(portal));

        var again = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: bill), Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("review_already_submitted", (await ErrorAsync(again)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Someone_elses_visit_is_not_found_and_an_old_one_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var theirs = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 2, Token, other.CustomerId, other.PetId);
        var old = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 61, Token);

        using var portal = fixture.Portal(clinic);

        var notMine = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: theirs), Token);
        Assert.Equal(HttpStatusCode.NotFound, notMine.StatusCode);

        var unknown = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: "inv_nothing"), Token);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var tooOld = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: old), Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooOld.StatusCode);
        Assert.Equal("visit_id", (await ErrorAsync(tooOld)).GetProperty("field").GetString());

        // The other customer can still review their own.
        using var theirPortal = fixture.PortalAs(other.CustomerId);
        Assert.Equal(HttpStatusCode.Created, (await theirPortal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: theirs), Token)).StatusCode);
    }

    [Theory]
    [InlineData(0, "ดี", "first_name", "rating")]
    [InlineData(6, "ดี", "first_name", "rating")]
    [InlineData(5, "<b>ดีมาก</b>", "first_name", "text")]
    [InlineData(5, "ดี", "full_name", "display_name")]
    public async Task A_review_that_breaks_a_rule_names_the_field(int rating, string text, string displayName, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 1, Token);

        using var portal = fixture.Portal(clinic);
        var response = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(rating, text, displayName, bill), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await ErrorAsync(response);
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Fact]
    public async Task Text_longer_than_a_thousand_characters_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 1, Token);

        using var portal = fixture.Portal(clinic);
        var response = await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(5, new string('ก', 1_001), visitId: bill), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Staff_tokens_cannot_use_the_portal_routes()
    {
        using var staff = fixture.Moderator("stf_rev_portal");

        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/portal/reviews/pending", Token)).StatusCode);
    }
}
