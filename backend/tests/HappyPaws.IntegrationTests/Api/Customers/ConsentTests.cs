using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Customers;

/// <summary>
/// Consent, and the thing that makes it more than a stored preference: a message the clinic does
/// not send.
/// <para>
/// None of these endpoints are in the API reference — see <c>docs/contract-extensions.md § 4</c>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConsentTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Audit.Read] : scopes,
            [clinic.BranchId]));

    private static JsonElement Purpose(JsonElement summary, string purpose) =>
        summary.GetProperty("consents").EnumerateArray()
            .First(consent => consent.GetProperty("purpose").GetString() == purpose);

    [Fact]
    public async Task A_customer_who_has_never_been_asked_gets_the_defaults()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var summary = await owner.GetFromJsonAsync<JsonElement>("/portal/consents", Token);

        var service = Purpose(summary, "service_messages");
        var marketing = Purpose(summary, "marketing");

        // Care messaging on, everything else off — the only defensible direction for a default.
        Assert.True(service.GetProperty("granted").GetBoolean());
        Assert.False(marketing.GetProperty("granted").GetBoolean());

        // And "nobody ever asked" is reported as exactly that, not as agreement: the flag says so
        // and there is no date, because there was no decision to date.
        Assert.True(service.GetProperty("is_default").GetBoolean());
        Assert.False(service.TryGetProperty("recorded_at", out _));
    }

    [Fact]
    public async Task A_customer_can_opt_in_to_marketing_and_take_it_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var opted = await owner.PutAsJsonAsync(
            "/portal/consents",
            new { consents = new { marketing = true }, policy_version = "2026-01" },
            Token);

        Assert.Equal(HttpStatusCode.OK, opted.StatusCode);

        var afterOptIn = await opted.Content.ReadFromJsonAsync<JsonElement>(Token);
        var marketing = Purpose(afterOptIn, "marketing");

        Assert.True(marketing.GetProperty("granted").GetBoolean());
        Assert.False(marketing.GetProperty("is_default").GetBoolean());
        Assert.Equal("portal", marketing.GetProperty("source").GetString());

        // The version the client sends is not evidence of what was shown, so the server stamps the
        // notice current now (docs/contract-extensions.md § 35). Nothing is published in this
        // collection, so the row honestly carries none.
        Assert.False(marketing.TryGetProperty("policy_version", out _));

        var withdrawn = await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { marketing = false } }, Token);

        var afterWithdrawal = await withdrawn.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.False(Purpose(afterWithdrawal, "marketing").GetProperty("granted").GetBoolean());

        // Both decisions survive. "On what date did they agree, and when did they take it back" is
        // the question a complaint actually asks, and a mutable flag cannot answer it.
        var history = afterWithdrawal.GetProperty("history").EnumerateArray().ToList();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, row => row.GetProperty("granted").GetBoolean());
        Assert.Contains(history, row => !row.GetProperty("granted").GetBoolean());
    }

    [Fact]
    public async Task Re_sending_an_unchanged_position_does_not_add_a_row()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        await owner.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = true } }, Token);
        var again = await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { marketing = true } }, Token);

        var summary = await again.Content.ReadFromJsonAsync<JsonElement>(Token);

        // A portal screen posts every toggle it shows. Storing the unchanged ones would bury the
        // rows that matter.
        Assert.Single(summary.GetProperty("history").EnumerateArray());
    }

    [Fact]
    public async Task Confirming_a_default_is_recorded_because_being_asked_is_itself_a_fact()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { service_messages = true } }, Token);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        // "They were asked and said yes" is materially different from "nobody ever asked", and it
        // is the first one a clinic needs to be able to prove.
        Assert.Single(summary.GetProperty("history").EnumerateArray());
        Assert.False(Purpose(summary, "service_messages").GetProperty("is_default").GetBoolean());
    }

    [Fact]
    public async Task The_counter_can_record_a_paper_consent_and_the_row_names_who_recorded_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var response = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/consents",
            new { consents = new { marketing = true }, note = "แบบฟอร์มกระดาษเลขที่ 118" },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var entry = summary.GetProperty("history")[0];

        Assert.Equal("counter", entry.GetProperty("source").GetString());
        Assert.Equal(clinic.StaffUserId, entry.GetProperty("recorded_by_user_id").GetString());
        Assert.Equal("แบบฟอร์มกระดาษเลขที่ 118", entry.GetProperty("note").GetString());
    }

    [Fact]
    public async Task Every_consent_decision_is_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);
        using var auditor = Counter(clinic);

        await owner.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = true } }, Token);
        await owner.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = false } }, Token);

        var log = await auditor.GetFromJsonAsync<JsonElement>(
            "/staff/audit-logs?resource_type=consent", Token);

        var actions = log.GetProperty("data").EnumerateArray()
            .Where(row => row.GetProperty("details").GetProperty("customer_id").GetString() == clinic.CustomerId)
            .Select(row => row.GetProperty("action").GetString())
            .ToList();

        // Granted and withdrawn under separate actions, because the two are looked for separately.
        Assert.Contains("consent.recorded", actions);
        Assert.Contains("consent.withdrawn", actions);
    }

    [Fact]
    public async Task An_unknown_purpose_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { sell_to_advertisers = true } }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_portal_reads_the_signed_in_customer_and_not_a_supplied_id()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var owner = fixture.CustomerOf(clinic);
        await owner.PutAsJsonAsync("/portal/consents", new { consents = new { marketing = true } }, Token);

        using var counter = Counter(clinic);
        var theirs = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers/{otherCustomerId}/consents", Token);

        // The route takes no customer id at all, so there is nothing to tamper with.
        Assert.False(Purpose(theirs, "marketing").GetProperty("granted").GetBoolean());
        Assert.Empty(theirs.GetProperty("history").EnumerateArray());
    }

    [Fact]
    public async Task Reading_consent_takes_customer_read_and_writing_it_takes_customer_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = Counter(clinic, Scopes.Customer.Read);

        var read = await reader.GetAsync($"/staff/customers/{clinic.CustomerId}/consents", Token);
        var write = await reader.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/consents",
            new { consents = new { marketing = true } },
            Token);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }
}
