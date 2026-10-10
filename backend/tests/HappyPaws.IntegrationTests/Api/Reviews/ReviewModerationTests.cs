using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Reviews;

/// <summary>
/// <c>docs/contract-extensions.md § 36</c>: staff with <c>content:manage</c> publish, reject for a
/// listed reason, or answer; every decision is audited with their id; and the public list and its
/// average are published reviews only, with no personal data.
/// <para>
/// The suite shares one database, so the public figures are compared before and after rather than
/// asserted outright.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ReviewModerationTests(ApiFixture fixture)
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

    /// <summary>A visit reviewed in the portal by a freshly seeded customer, with a known name.</summary>
    private async Task<(SeededClinic Clinic, string ReviewId)> ReviewedAsync(int rating, string text, string displayName = "first_name")
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await ReviewScenario.RenameAsync(fixture, clinic.CustomerId, "คุณปิติ นามสกุลลับ", Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 2, Token);

        using var portal = fixture.Portal(clinic);
        var review = await OkAsync(await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(rating, text, displayName, bill), Token));

        return (clinic, review.GetProperty("id").GetString()!);
    }

    private async Task<JsonElement> PublicAsync(int limit = 100)
    {
        using var anonymous = fixture.CreateClient();
        return await OkAsync(await anonymous.GetAsync($"/reviews?limit={limit}", Token));
    }

    private static List<string> IdsIn(JsonElement page) =>
        [.. page.GetProperty("data").EnumerateArray().Select(review => review.GetProperty("id").GetString()!)];

    private async Task<List<AuditEntry>> AuditAsync(string reviewId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.Review && entry.ResourceId == reviewId)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync(Token);
    }

    [Fact]
    public async Task Every_staff_route_needs_content_manage()
    {
        var (_, reviewId) = await ReviewedAsync(5, "ดี");
        using var other = fixture.Moderator("stf_rev_none", Scopes.Customer.Read);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync("/staff/reviews", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/staff/reviews/{reviewId}/reject", new { reason = "spam" }, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/staff/reviews/{reviewId}/reply", new { text = "ขอบคุณ" }, Token)).StatusCode);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/reviews", Token)).StatusCode);
    }

    [Fact]
    public async Task A_pending_review_is_off_the_public_site_until_staff_publish_it()
    {
        var (_, reviewId) = await ReviewedAsync(5, "หมอใจดีมาก รอไม่นาน");
        var before = await PublicAsync();
        Assert.DoesNotContain(reviewId, IdsIn(before));

        using var moderator = fixture.Moderator("stf_rev_pub");
        var pending = await OkAsync(await moderator.GetAsync("/staff/reviews?status=pending&limit=100", Token));
        var row = pending.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == reviewId);
        Assert.Equal("คุณปิติ นามสกุลลับ", row.GetProperty("customer").GetProperty("name").GetString());
        Assert.Equal("ลูน่า", row.GetProperty("visit").GetProperty("pet_name").GetString());

        var published = await OkAsync(await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token));
        Assert.Equal("published", published.GetProperty("status").GetString());
        Assert.Equal("stf_rev_pub", published.GetProperty("moderation").GetProperty("moderated_by_staff_id").GetString());

        var after = await PublicAsync();
        Assert.Contains(reviewId, IdsIn(after));
        Assert.Equal(before.GetProperty("summary").GetProperty("count").GetInt32() + 1, after.GetProperty("summary").GetProperty("count").GetInt32());
        Assert.Equal(
            before.GetProperty("summary").GetProperty("by_rating").GetProperty("5").GetInt32() + 1,
            after.GetProperty("summary").GetProperty("by_rating").GetProperty("5").GetInt32());

        var again = await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(again)).GetProperty("code").GetString());

        var audit = Assert.Single(await AuditAsync(reviewId));
        Assert.Equal(AuditActions.ReviewPublished, audit.Action);
        Assert.Equal("stf_rev_pub", audit.ActorStaffId);
    }

    [Fact]
    public async Task The_public_list_shows_a_first_name_and_species_and_never_the_rest()
    {
        var (_, reviewId) = await ReviewedAsync(4, "บริการดี");
        using var moderator = fixture.Moderator("stf_rev_public");
        await OkAsync(await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token));

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync("/reviews?limit=100", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);
        var page = await OkAsync(response);
        var review = page.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == reviewId);

        Assert.Equal("ปิติ", review.GetProperty("display_name").GetString());
        Assert.Equal("สุนัข", review.GetProperty("pet_species").GetString());
        Assert.Equal(4, review.GetProperty("rating").GetInt32());
        Assert.False(review.TryGetProperty("customer", out _));
        Assert.False(review.TryGetProperty("pet_name", out _));
        Assert.DoesNotContain("นามสกุลลับ", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("ลูน่า", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("phone", raw, StringComparison.Ordinal);
        Assert.Contains("public", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejecting_needs_a_reason_from_the_list_and_takes_a_review_out_of_the_average()
    {
        var (_, reviewId) = await ReviewedAsync(1, "ไม่ประทับใจ");
        using var moderator = fixture.Moderator("stf_rev_rej");
        await OkAsync(await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token));
        var published = await PublicAsync();
        Assert.Contains(reviewId, IdsIn(published));

        var noReason = await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reject", new { }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
        Assert.Equal("reason", (await ErrorAsync(noReason)).GetProperty("field").GetString());

        var dislike = await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reject", new { reason = "ไม่ชอบ" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dislike.StatusCode);

        var rejected = await OkAsync(await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reject", new { reason = "customer_request" }, Token));
        Assert.Equal("rejected", rejected.GetProperty("status").GetString());
        Assert.Equal("customer_request", rejected.GetProperty("moderation").GetProperty("reject_reason").GetString());
        Assert.Equal("ลูกค้าขอให้นำออก", rejected.GetProperty("moderation").GetProperty("reject_reason_label").GetString());

        var after = await PublicAsync();
        Assert.DoesNotContain(reviewId, IdsIn(after));
        Assert.Equal(published.GetProperty("summary").GetProperty("count").GetInt32() - 1, after.GetProperty("summary").GetProperty("count").GetInt32());

        Assert.Equal(HttpStatusCode.Conflict, (await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reply", new { text = "ขอโทษค่ะ" }, Token)).StatusCode);

        var audit = await AuditAsync(reviewId);
        Assert.Equal([AuditActions.ReviewPublished, AuditActions.ReviewRejected], audit.Select(entry => entry.Action));
        Assert.All(audit, entry => Assert.Equal("stf_rev_rej", entry.ActorStaffId));

        using var rejectedTab = fixture.Moderator("stf_rev_rej");
        var tab = await OkAsync(await rejectedTab.GetAsync("/staff/reviews?status=rejected&limit=100", Token));
        Assert.Contains(reviewId, IdsIn(tab));
    }

    [Fact]
    public async Task The_clinic_answers_once_and_can_rewrite_its_answer_but_not_the_customers_words()
    {
        var (_, reviewId) = await ReviewedAsync(3, "รอนานไปหน่อย");
        using var moderator = fixture.Moderator("stf_rev_reply");

        var empty = await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reply", new { text = "  " }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);

        await OkAsync(await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reply", new { text = "ขอบคุณค่ะ" }, Token));
        var rewritten = await OkAsync(await moderator.PostAsJsonAsync($"/staff/reviews/{reviewId}/reply", new { text = "ขอบคุณค่ะ เราจะเพิ่มคนช่วงเช้า" }, Token));

        Assert.Equal("ขอบคุณค่ะ เราจะเพิ่มคนช่วงเช้า", rewritten.GetProperty("reply").GetProperty("text").GetString());
        Assert.Equal("รอนานไปหน่อย", rewritten.GetProperty("text").GetString());
        Assert.Equal(3, rewritten.GetProperty("rating").GetInt32());

        await OkAsync(await moderator.PostAsync($"/staff/reviews/{reviewId}/publish", null, Token));
        var review = (await PublicAsync()).GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == reviewId);
        Assert.Equal("ขอบคุณค่ะ เราจะเพิ่มคนช่วงเช้า", review.GetProperty("reply").GetProperty("text").GetString());

        var audit = await AuditAsync(reviewId);
        Assert.Equal(2, audit.Count(entry => entry.Action == AuditActions.ReviewReplied));
        Assert.All(audit, entry => Assert.Equal("stf_rev_reply", entry.ActorStaffId));
    }

    [Fact]
    public async Task An_unknown_tab_is_refused_rather_than_answered_with_everything()
    {
        using var moderator = fixture.Moderator("stf_rev_tab");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await moderator.GetAsync("/staff/reviews?status=hidden", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await moderator.PostAsync("/staff/reviews/rev_nothing/publish", null, Token)).StatusCode);
    }

    [Fact]
    public async Task The_public_list_pages_newest_first()
    {
        var first = await ReviewedAsync(5, "หนึ่ง");
        var second = await ReviewedAsync(4, "สอง");
        using var moderator = fixture.Moderator("stf_rev_page");
        await OkAsync(await moderator.PostAsync($"/staff/reviews/{first.ReviewId}/publish", null, Token));
        await OkAsync(await moderator.PostAsync($"/staff/reviews/{second.ReviewId}/publish", null, Token));

        using var anonymous = fixture.CreateClient();
        var page = await OkAsync(await anonymous.GetAsync("/reviews?limit=1", Token));
        Assert.Equal([second.ReviewId], IdsIn(page));

        var cursor = page.GetProperty("page").GetProperty("next_cursor").GetString();
        Assert.False(string.IsNullOrEmpty(cursor));
        var next = await OkAsync(await anonymous.GetAsync($"/reviews?limit=1&cursor={cursor}", Token));
        Assert.Equal([first.ReviewId], IdsIn(next));
    }
}
