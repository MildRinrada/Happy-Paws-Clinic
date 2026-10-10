using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.PrivacyPolicies;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.Infrastructure.Persistence.Migrations;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.PrivacyPolicyScenario;

namespace HappyPaws.IntegrationTests.Api.PrivacyPolicies;

/// <summary>
/// <c>docs/contract-extensions.md § 35</c> before anything is published: the template is seeded as
/// a draft and only a draft, the public page has nothing to show but says so, consents are still
/// recorded, and drafts are written under <c>settings:write</c> with <c>If-Match</c>.
/// <para>
/// In <see cref="ApiCollection"/> on purpose, and nothing here publishes: no test in this
/// collection ever publishes a notice, which is what makes "nothing published yet" observable.
/// Publishing is tested in <see cref="PrivacyPolicyCollection"/>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class PrivacyPolicyDraftTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static object Sections(params object[] sections) => new { sections };

    [Fact]
    public async Task The_migration_seeds_the_template_as_a_draft_and_nothing_public()
    {
        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var seeded = await db.Set<PrivacyPolicyVersion>().AsNoTracking()
                .SingleAsync(version => version.Id == ClinicPrivacyPolicy.SeededDraftId, Token);

            Assert.False(seeded.IsPublished);
            Assert.Null(seeded.Label);
            Assert.Equal(PrivacyPolicySource.Template, seeded.CreatedFrom);
            Assert.Equal(PrivacyPolicyTemplate.Version, seeded.TemplateVersion);
            Assert.Equal(PrivacyPolicyTemplate.Sections, seeded.Sections);
            Assert.False(await db.Set<PrivacyPolicyVersion>().AnyAsync(version => version.PublishedAt != null, Token));
        }

        using var staff = fixture.Writer();
        var list = await ReadAsync(await staff.GetAsync(Versions, Token), Token);

        Assert.False(list.TryGetProperty("current_version_id", out _));
        Assert.Equal(PrivacyPolicyTemplate.Version, list.GetProperty("template").GetProperty("version").GetString());
        Assert.False(list.GetProperty("template").GetProperty("newer_available").GetBoolean());

        var row = list.GetProperty("data").EnumerateArray().Single(version => IdOf(version) == ClinicPrivacyPolicy.SeededDraftId);
        Assert.Equal("draft", row.GetProperty("status").GetString());
        Assert.Equal("template", row.GetProperty("created_from").GetString());
        Assert.False(row.GetProperty("is_current").GetBoolean());
        Assert.Equal(PrivacyPolicyTemplate.Sections.Count, row.GetProperty("sections").GetArrayLength());
    }

    [Fact]
    public async Task Before_anything_is_published_the_public_read_says_so_and_never_shows_the_template()
    {
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync("/privacy-policy", Token);
        var body = await ReadAsync(response, Token);

        Assert.False(body.GetProperty("published").GetBoolean());
        Assert.False(body.TryGetProperty("sections", out _));
        Assert.Equal(0, body.GetProperty("history").GetArrayLength());
        Assert.Contains("max-age=60", response.Headers.CacheControl?.ToString());

        // A draft is never public, by label or by id.
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/privacy-policy/versions/{ClinicPrivacyPolicy.SeededDraftId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/privacy-policy/versions/{PrivacyPolicyRules.LegacyLabel}", Token)).StatusCode);
    }

    [Fact]
    public async Task A_consent_is_still_recorded_with_no_version_while_nothing_is_published()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var saved = await ReadAsync(
            await owner.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = true }, policy_version = "made-up" }, Token),
            Token);

        var marketing = saved.GetProperty("consents").EnumerateArray().Single(row => row.GetProperty("purpose").GetString() == "marketing");
        Assert.True(marketing.GetProperty("granted").GetBoolean());
        Assert.False(marketing.TryGetProperty("policy_version", out _));
    }

    [Fact]
    public async Task Each_route_needs_its_scope()
    {
        using var anonymous = fixture.CreateClient();
        using var reader = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Read]));
        using var writer = fixture.Writer();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Versions, Token)).StatusCode);

        using var none = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Customer.Read]));
        Assert.Equal(HttpStatusCode.Forbidden, (await none.GetAsync(Versions, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync(Versions, Token)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync(Versions, new { source = "template" }, Token)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendAsync(reader, HttpMethod.Patch, $"{Versions}/{ClinicPrivacyPolicy.SeededDraftId}", Sections(), 1, Token)).StatusCode);

        // settings:write drafts but never publishes; the refusal is the scope check, before the draft is read.
        var draft = await CreateDraftAsync(writer, Token);
        var refused = await PublishAsync(writer, draft, UniqueLabel(), Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        var error = await ErrorAsync(refused, Token);
        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Contains(
            Scopes.Settings.Danger,
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(scope => scope.GetString()));

        // And the starter clinic_admin, who holds settings:write, does not hold settings:danger.
        var admin = DefaultRoles.StarterScopes.Single(role => role.Key == DefaultRoles.ClinicAdmin).Value;
        Assert.Contains(Scopes.Settings.Write, admin);
        Assert.DoesNotContain(Scopes.Settings.Danger, admin);
    }

    [Fact]
    public async Task A_draft_is_made_from_the_template_or_a_version_and_audited()
    {
        using var writer = fixture.Writer();

        var fromTemplate = await CreateDraftAsync(writer, Token);
        Assert.Equal("draft", fromTemplate.GetProperty("status").GetString());
        Assert.Equal("template", fromTemplate.GetProperty("created_from").GetString());
        Assert.Equal("stf_writer", fromTemplate.GetProperty("created_by_staff_id").GetString());

        var copy = await CreateDraftAsync(writer, Token, "version", IdOf(fromTemplate));
        Assert.Equal("version", copy.GetProperty("created_from").GetString());
        Assert.Equal(IdOf(fromTemplate), copy.GetProperty("based_on_version_id").GetString());
        Assert.Equal(PrivacyPolicyTemplate.Version, copy.GetProperty("template_version").GetString());

        // "current" needs a published version, and there is none in this collection.
        var noCurrent = await writer.PostAsJsonAsync(Versions, new { source = "current" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noCurrent.StatusCode);
        Assert.Equal("source", (await ErrorAsync(noCurrent, Token)).GetProperty("field").GetString());

        var unknown = await writer.PostAsJsonAsync(Versions, new { source = "version", version_id = "ppv_missing" }, Token);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var badSource = await writer.PostAsJsonAsync(Versions, new { source = "blank" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badSource.StatusCode);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var audited = await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.PrivacyPolicy && entry.ResourceId == IdOf(copy))
            .ToListAsync(Token);

        Assert.Equal(AuditActions.PrivacyPolicyDraftCreated, Assert.Single(audited).Action);
    }

    [Fact]
    public async Task A_draft_edit_needs_the_version_it_was_read_at_and_valid_sections()
    {
        using var writer = fixture.Writer();
        var draft = await CreateDraftAsync(writer, Token);
        var path = $"{Versions}/{IdOf(draft)}";
        var body = Sections(new { anchor = "cookies", heading = "คุกกี้", body = "ย่อหน้า\n\n- [บัญชีของฉัน](/portal/account)" });

        var missing = await SendAsync(writer, HttpMethod.Patch, path, body, ifMatch: null, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        Assert.Equal("If-Match", (await ErrorAsync(missing, Token)).GetProperty("field").GetString());

        var stale = await SendAsync(writer, HttpMethod.Patch, path, body, VersionOf(draft) + 1, Token);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", (await ErrorAsync(stale, Token)).GetProperty("code").GetString());

        var saved = await ReadAsync(await SendAsync(writer, HttpMethod.Patch, path, body, VersionOf(draft), Token), Token);
        Assert.Equal("cookies", Assert.Single(saved.GetProperty("sections").EnumerateArray()).GetProperty("anchor").GetString());
        Assert.NotEqual(VersionOf(draft), VersionOf(saved));

        // The version just read is stale after the save: a second writer holding it loses.
        var second = await SendAsync(writer, HttpMethod.Patch, path, body, VersionOf(draft), Token);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var external = await SendAsync(
            writer,
            HttpMethod.Patch,
            path,
            Sections(new { anchor = "cookies", heading = "คุกกี้", body = "[ไปที่](https://evil.example)" }),
            VersionOf(saved),
            Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, external.StatusCode);
        Assert.Equal("sections[0].body", (await ErrorAsync(external, Token)).GetProperty("field").GetString());

        var html = await SendAsync(
            writer,
            HttpMethod.Patch,
            path,
            Sections(
                new { anchor = "a", heading = "หนึ่ง", body = "ข้อความ" },
                new { anchor = "a", heading = "สอง", body = "<script>alert(1)</script>" }),
            VersionOf(saved),
            Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, html.StatusCode);
        var errors = (await ErrorAsync(html, Token)).GetProperty("details").GetProperty("errors");
        Assert.True(errors.TryGetProperty("sections[1].anchor", out _));
        Assert.True(errors.TryGetProperty("sections[1].body", out _));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var actions = await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.PrivacyPolicy && entry.ResourceId == IdOf(draft))
            .Select(entry => entry.Action)
            .ToListAsync(Token);

        Assert.Equal(1, actions.Count(action => action == AuditActions.PrivacyPolicyDraftUpdated));
    }
}
