using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Reviews;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Reviews;

/// <summary>
/// <c>docs/contract-extensions.md § 36</c>: a customer with no portal account reviews a visit
/// through a one-time link. The link names the visit and nothing personal, works once, ends after
/// 30 days, and is kept only as a hash.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ReviewInvitationTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString();

    [Fact]
    public async Task The_link_names_the_visit_and_nothing_personal()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 3, Token);
        var token = await ReviewScenario.InvitationAsync(fixture, clinic, bill, visitDaysAgo: 3, Token);

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync($"/reviews/invitations/{token}", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);
        var invitation = await OkAsync(response);

        Assert.Equal("usable", invitation.GetProperty("status").GetString());
        Assert.Equal(ReviewScenario.Today.AddDays(-3).ToString("yyyy-MM-dd"), invitation.GetProperty("visit").GetProperty("visit_on").GetString());
        Assert.Equal("ตรวจสุขภาพ", invitation.GetProperty("visit").GetProperty("service_name").GetString());
        Assert.Equal("ลูน่า", invitation.GetProperty("visit").GetProperty("pet_name").GetString());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        Assert.DoesNotContain("ลูกค้า ทดสอบ", raw, StringComparison.Ordinal);
        Assert.DoesNotContain(clinic.CustomerId, raw, StringComparison.Ordinal);
        Assert.DoesNotContain("customer", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("phone", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_link_writes_one_review_and_is_used_after()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 4, Token);
        var token = await ReviewScenario.InvitationAsync(fixture, clinic, bill, visitDaysAgo: 4, Token);

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.PostAsJsonAsync($"/reviews/invitations/{token}", ReviewScenario.Body(5, "ดีมาก", "anonymous"), Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var review = await OkAsync(response);
        Assert.Equal("pending", review.GetProperty("status").GetString());
        Assert.Equal("ลูกค้า", review.GetProperty("display_name").GetString());

        var used = await OkAsync(await anonymous.GetAsync($"/reviews/invitations/{token}", Token));
        Assert.Equal("used", used.GetProperty("status").GetString());

        var again = await anonymous.PostAsJsonAsync($"/reviews/invitations/{token}", ReviewScenario.Body(1, "ลองอีกครั้ง"), Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("review_already_submitted", await CodeOf(again));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var stored = await db.Set<CustomerReview>().AsNoTracking().SingleAsync(row => row.VisitId == bill, Token);
        Assert.Equal(ReviewSource.Invitation, stored.Source);
        Assert.Equal(clinic.CustomerId, stored.CustomerId);
        Assert.Equal(5, stored.Rating);
    }

    [Fact]
    public async Task A_link_older_than_thirty_days_has_expired()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 40, Token);
        var token = await ReviewScenario.InvitationAsync(fixture, clinic, bill, visitDaysAgo: 40, Token, madeDaysAgo: 31);

        using var anonymous = fixture.CreateClient();
        Assert.Equal("expired", (await OkAsync(await anonymous.GetAsync($"/reviews/invitations/{token}", Token))).GetProperty("status").GetString());

        var response = await anonymous.PostAsJsonAsync($"/reviews/invitations/{token}", ReviewScenario.Body(), Token);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("review_invitation_expired", await CodeOf(response));
    }

    [Fact]
    public async Task A_visit_reviewed_in_the_portal_uses_up_its_link()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 2, Token);
        var token = await ReviewScenario.InvitationAsync(fixture, clinic, bill, visitDaysAgo: 2, Token);

        using var portal = fixture.Portal(clinic);
        await OkAsync(await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: bill), Token));

        using var anonymous = fixture.CreateClient();
        Assert.Equal("used", (await OkAsync(await anonymous.GetAsync($"/reviews/invitations/{token}", Token))).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await anonymous.PostAsJsonAsync($"/reviews/invitations/{token}", ReviewScenario.Body(), Token)).StatusCode);
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("hprv_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task An_unknown_link_is_not_found(string token)
    {
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/reviews/invitations/{token}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsJsonAsync($"/reviews/invitations/{token}", ReviewScenario.Body(), Token)).StatusCode);
    }

    [Fact]
    public async Task Only_the_hash_of_the_token_is_stored()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 1, Token);
        var token = await ReviewScenario.InvitationAsync(fixture, clinic, bill, visitDaysAgo: 1, Token);

        var stored = Assert.Single(await ReviewScenario.InvitationsOfAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(ReviewInvitation.Hash(token), stored.TokenHash);

        // No column of the row holds the token itself.
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var matches = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM review_invitations r WHERE r.id = {stored.Id} AND row_to_json(r)::text LIKE {"%" + token + "%"}")
            .SingleAsync(Token);
        Assert.Equal(0, matches);
    }
}
