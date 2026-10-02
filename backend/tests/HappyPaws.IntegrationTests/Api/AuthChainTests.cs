using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// The single most load-bearing integration in the system: a token minted by
/// <c>IAccessTokenIssuer</c> must be accepted by the bearer validator and read back correctly as
/// <c>ICurrentUser</c>.
/// <para>
/// If any claim name disagrees between the two ends, the caller arrives with no id and no scopes —
/// and every scope check then passes vacuously or fails universally, silently, with no exception
/// anywhere. These tests exist to make that impossible to ship.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class AuthChainTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_issued_token_round_trips_into_the_current_user()
    {
        var token = TestTokens.Staff(
            fixture,
            userId: "usr_8fA2",
            staffId: "stf_204",
            scopes: [Scopes.Consult.Write, Scopes.Record.Write],
            branches: ["brn_sukhumvit39", "brn_thonglor"]);

        using var client = fixture.Authenticated(token);

        var body = await client.GetFromJsonAsync<JsonElement>("/_probe/identity", Token);

        Assert.True(body.GetProperty("authenticated").GetBoolean());
        Assert.Equal("usr_8fA2", body.GetProperty("user_id").GetString());
        Assert.Equal("stf_204", body.GetProperty("staff_id").GetString());

        // Null members are omitted from responses, not sent as null — see docs/api-mapping.md.
        // A staff-only token must not carry a customer membership at all.
        Assert.False(body.TryGetProperty("customer_id", out _));

        var branches = body.GetProperty("branches").EnumerateArray().Select(b => b.GetString()!).ToArray();
        Assert.Equal(["brn_sukhumvit39", "brn_thonglor"], branches);

        var scopes = body.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).ToArray();
        Assert.Contains(Scopes.Consult.Write, scopes);
        Assert.Contains(Scopes.Record.Write, scopes);
    }

    [Fact]
    public async Task A_customer_token_carries_a_customer_membership_and_no_staff_id()
    {
        using var client = fixture.Authenticated(TestTokens.Customer(fixture));

        var body = await client.GetFromJsonAsync<JsonElement>("/_probe/identity", Token);

        Assert.Equal("cus_test", body.GetProperty("customer_id").GetString());
        Assert.False(body.TryGetProperty("staff_id", out _));
    }

    [Fact]
    public async Task Public_endpoints_need_no_token()
    {
        using var client = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/public", Token)).StatusCode);
    }

    [Fact]
    public async Task A_sufficient_scope_is_allowed()
    {
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Appointment.Read]));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/scoped", Token)).StatusCode);
    }

    [Fact]
    public async Task A_resource_wildcard_satisfies_an_action_scope_over_the_wire()
    {
        // Proves the Domain wildcard rules are the ones actually enforced at the boundary,
        // not a second implementation that happens to look similar.
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: ["appointment:*"]));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/scoped", Token)).StatusCode);
    }

    [Fact]
    public async Task The_owner_wildcard_satisfies_everything()
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Wildcard]));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/scoped", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/sensitive", Token)).StatusCode);
    }

    [Fact]
    public async Task A_missing_scope_is_refused_with_403_naming_what_is_missing()
    {
        // The contract requires the response to say which scope was missing
        // ("พร้อมบอก scope ที่ขาด").
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Queue.Read]));

        var response = await client.GetAsync("/_probe/scoped", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(ErrorCodes.InsufficientScope, error.GetProperty("code").GetString());

        var missing = error.GetProperty("details").GetProperty("missing_scopes")
            .EnumerateArray().Select(s => s.GetString()!).ToArray();
        Assert.Equal([Scopes.Appointment.Read], missing);
    }

    [Fact]
    public async Task An_unrelated_wildcard_does_not_leak_across_resources_over_the_wire()
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: ["queue:*"]));

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/_probe/scoped", Token)).StatusCode);
    }

    [Fact]
    public async Task A_medical_scope_without_2fa_is_refused()
    {
        // The contract: staff at medical-record level and above must pass 2FA. The gate is on the
        // scopes the token carries, not on a role name, so a custom role inherits it.
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Record.Sign], twoFactorSatisfied: false));

        var response = await client.GetAsync("/_probe/sensitive", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("two_factor", error.GetProperty("details").GetProperty("required").GetString());
    }

    [Fact]
    public async Task A_medical_scope_with_2fa_is_allowed()
    {
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Record.Sign], twoFactorSatisfied: true));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/sensitive", Token)).StatusCode);
    }

    [Fact]
    public async Task A_non_medical_scope_does_not_require_2fa()
    {
        // The gate must not be a blanket "2FA for everything", or a receptionist could not take a
        // booking without enrolling.
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Appointment.Read], twoFactorSatisfied: false));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/scoped", Token)).StatusCode);
    }

    [Fact]
    public async Task Branch_scoping_refuses_a_branch_the_staff_is_not_assigned_to()
    {
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Patient.Read], branches: ["brn_sukhumvit39"]));

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/_probe/branch/brn_sukhumvit39", Token)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync("/_probe/branch/brn_other", Token)).StatusCode);
    }

    [Fact]
    public async Task An_unrestricted_caller_is_not_branch_bound()
    {
        using var client = fixture.Authenticated(
            TestTokens.Staff(fixture, scopes: [Scopes.Wildcard], branches: ["brn_sukhumvit39"]));

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/_probe/branch/brn_other", Token)).StatusCode);
    }
}
