using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.PrivacyPolicies;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.PrivacyPolicyScenario;

namespace HappyPaws.IntegrationTests.Api.PrivacyPolicies;

/// <summary>
/// <c>docs/contract-extensions.md § 35</c>: only the owner publishes, a published version never
/// changes, the newest published one is what the public page shows and what every consent is
/// stamped with, and the older ones stay readable by label.
/// <para>
/// Its own instance (<see cref="PrivacyPolicyCollection"/>), because publishing changes what every
/// consent records. The tests run one at a time but in no promised order, so each publishes its
/// own labels and asserts on the version it just published.
/// </para>
/// </summary>
[Collection(nameof(PrivacyPolicyCollection))]
public class PrivacyPolicyPublishingTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<JsonElement> PublicAsync(string path = "/privacy-policy")
    {
        using var anonymous = fixture.CreateClient();
        return await ReadAsync(await anonymous.GetAsync(path, Token), Token);
    }

    [Fact]
    public async Task The_owner_publishes_and_the_public_page_shows_that_text()
    {
        using var writer = fixture.Writer();
        using var danger = fixture.Authenticated(TestTokens.Staff(fixture, staffId: "stf_danger", scopes: [Scopes.Settings.Write, Scopes.Settings.Danger]));

        var draft = await CreateDraftAsync(writer, Token);
        var label = UniqueLabel("owner");

        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(writer, draft, label, Token)).StatusCode);

        var published = await ReadAsync(await PublishAsync(danger, draft, label, Token, legalReviewDone: true), Token);
        Assert.Equal("published", published.GetProperty("status").GetString());
        Assert.Equal(label, published.GetProperty("label").GetString());
        Assert.Equal(Today(), published.GetProperty("effective_date").GetString());
        Assert.Equal("stf_danger", published.GetProperty("published_by_staff_id").GetString());
        Assert.True(published.GetProperty("is_current").GetBoolean());

        var page = await PublicAsync();
        Assert.True(page.GetProperty("published").GetBoolean());
        Assert.Equal(label, page.GetProperty("label").GetString());
        Assert.True(page.GetProperty("is_current").GetBoolean());
        Assert.True(page.GetProperty("legal_review_done").GetBoolean());
        Assert.Equal(PrivacyPolicyTemplate.Sections.Count, page.GetProperty("sections").GetArrayLength());
        Assert.Equal(label, page.GetProperty("history")[0].GetProperty("label").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var actions = await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.PrivacyPolicy && entry.ResourceId == IdOf(draft))
            .Select(entry => entry.Action)
            .ToListAsync(Token);

        Assert.Contains(AuditActions.PrivacyPolicyPublished, actions);

        var list = await ReadAsync(await writer.GetAsync(Versions, Token), Token);
        Assert.Equal(IdOf(draft), list.GetProperty("current_version_id").GetString());
    }

    [Fact]
    public async Task A_published_version_can_never_be_edited_or_published_again()
    {
        using var owner = fixture.Owner();
        var published = await PublishedAsync(owner, UniqueLabel("fixed"), Token);
        var path = $"{Versions}/{IdOf(published)}";

        var edit = await SendAsync(
            owner,
            HttpMethod.Patch,
            path,
            new { sections = new[] { new { anchor = "cookies", heading = "คุกกี้", body = "แก้แล้ว" } } },
            VersionOf(published),
            Token);
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(edit, Token)).GetProperty("code").GetString());

        var again = await PublishAsync(owner, published, UniqueLabel("again"), Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(again, Token)).GetProperty("code").GetString());

        // Changing the words means a new version: a draft copied from the current one.
        var next = await CreateDraftAsync(owner, Token, "current");
        Assert.Equal("draft", next.GetProperty("status").GetString());
        Assert.Equal("version", next.GetProperty("created_from").GetString());
    }

    [Fact]
    public async Task Consents_carry_the_current_version_whatever_the_client_sends()
    {
        using var owner = fixture.Owner();
        var label = UniqueLabel("stamp");
        await PublishedAsync(owner, label, Token);

        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);

        var saved = await ReadAsync(
            await customer.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = true }, policy_version = "1999-01" }, Token),
            Token);
        var marketing = saved.GetProperty("consents").EnumerateArray().Single(row => row.GetProperty("purpose").GetString() == "marketing");
        Assert.Equal(label, marketing.GetProperty("policy_version").GetString());

        // The counter's route is stamped the same way, with or without a version in the body.
        using var counter = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Customer.Read, Scopes.Customer.Write], [clinic.BranchId]));
        var recorded = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/consents",
            new { consents = new { medical_record_sharing = true }, note = "แบบฟอร์ม A-12" },
            Token);
        await ReadAsync(recorded, Token);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var rows = await db.Set<CustomerConsent>().AsNoTracking()
            .Where(row => row.CustomerId == clinic.CustomerId)
            .ToListAsync(Token);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(label, row.PolicyVersion));
    }

    [Fact]
    public async Task An_older_version_stays_readable_by_label_after_a_newer_one()
    {
        using var owner = fixture.Owner();
        var first = UniqueLabel("old");
        var second = UniqueLabel("new");

        var older = await PublishedAsync(owner, first, Token);

        // The newer text differs, so the two reads can be told apart.
        var draft = await CreateDraftAsync(owner, Token, "current");
        var edited = await ReadAsync(
            await SendAsync(
                owner,
                HttpMethod.Patch,
                $"{Versions}/{IdOf(draft)}",
                new { sections = new[] { new { anchor = "changes", heading = "ฉบับใหม่", body = "ข้อความฉบับใหม่" } } },
                VersionOf(draft),
                Token),
            Token);
        await ReadAsync(await PublishAsync(owner, edited, second, Token), Token);

        var current = await PublicAsync();
        Assert.Equal(second, current.GetProperty("label").GetString());
        Assert.Equal("ฉบับใหม่", current.GetProperty("sections")[0].GetProperty("heading").GetString());

        var past = await PublicAsync($"/privacy-policy/versions/{first}");
        Assert.True(past.GetProperty("published").GetBoolean());
        Assert.False(past.GetProperty("is_current").GetBoolean());
        Assert.Equal(older.GetProperty("sections").GetArrayLength(), past.GetProperty("sections").GetArrayLength());

        var history = current.GetProperty("history").EnumerateArray().Select(row => row.GetProperty("label").GetString()).ToList();
        Assert.True(history.IndexOf(second) < history.IndexOf(first));

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/privacy-policy/versions/{IdOf(draft)}x", Token)).StatusCode);
    }

    [Fact]
    public async Task A_publish_is_checked_label_date_and_version()
    {
        using var owner = fixture.Owner();
        var taken = UniqueLabel("taken");
        await PublishedAsync(owner, taken, Token);

        var draft = await CreateDraftAsync(owner, Token);

        var clash = await PublishAsync(owner, draft, taken, Token);
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        var error = await ErrorAsync(clash, Token);
        Assert.Equal("privacy_policy_label_taken", error.GetProperty("code").GetString());
        Assert.Equal("label", error.GetProperty("field").GetString());

        var legacy = await PublishAsync(owner, draft, PrivacyPolicyRules.LegacyLabel, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, legacy.StatusCode);
        Assert.Equal("label", (await ErrorAsync(legacy, Token)).GetProperty("field").GetString());

        var yesterday = ClinicSeed.ClinicDate(DateTimeOffset.UtcNow.AddDays(-1));
        var past = await PublishAsync(owner, draft, UniqueLabel(), Token, effectiveDate: yesterday);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, past.StatusCode);
        Assert.Equal("effective_date", (await ErrorAsync(past, Token)).GetProperty("field").GetString());

        var noVersion = await PublishAsync(owner, draft, UniqueLabel(), Token, sendVersion: false);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noVersion.StatusCode);
        Assert.Equal("If-Match", (await ErrorAsync(noVersion, Token)).GetProperty("field").GetString());

        // A future date is accepted and printed; the version is current from the moment of publishing.
        var later = ClinicSeed.ClinicDate(DateTimeOffset.UtcNow.AddDays(14));
        var label = UniqueLabel("later");
        var published = await ReadAsync(await PublishAsync(owner, draft, label, Token, effectiveDate: later), Token);
        Assert.Equal(later, published.GetProperty("effective_date").GetString());
        Assert.Equal(label, (await PublicAsync()).GetProperty("label").GetString());
    }
}
