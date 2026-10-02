using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Identity;

/// <summary>
/// The staff sign-in chain end to end: password, second factor, access token, <c>/me</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffAuthTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static HttpClient Client(ApiFixture fixture) =>
        fixture.CreateClient(); // cookie handling is on by default in WebApplicationFactory clients

    [Fact]
    public async Task An_account_without_2fa_signs_in_and_reaches_me()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Appointment.Read], withTwoFactor: false, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>(Token);
        var accessToken = tokens.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(tokens.GetProperty("two_factor_satisfied").GetBoolean());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var me = await client.GetFromJsonAsync<JsonElement>("/me", Token);

        Assert.Equal(staff.UserId, me.GetProperty("user_id").GetString());
        Assert.Equal("active", me.GetProperty("status").GetString());

        var membership = me.GetProperty("memberships").EnumerateArray().Single();
        Assert.Equal("staff", membership.GetProperty("type").GetString());
        Assert.Equal(staff.StaffId, membership.GetProperty("staff_id").GetString());
        Assert.Contains(
            Scopes.Appointment.Read,
            membership.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()));
    }

    [Fact]
    public async Task The_refresh_token_never_appears_in_the_response_body()
    {
        // It belongs in an httpOnly cookie. Returning it in the body too would hand script the
        // exact value the cookie flag exists to hide.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);

        var raw = await login.Content.ReadAsStringAsync(Token);

        Assert.DoesNotContain("refresh", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hp_refresh", string.Join(";", login.Headers.GetValues("Set-Cookie")));
        Assert.Contains("httponly", string.Join(";", login.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_enrolled_account_gets_a_challenge_and_no_access_token()
    {
        // The contract is explicit: /auth/2fa/verify is what returns the real access token.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Record.Sign], withTwoFactor: true, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);

        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.True(body.GetProperty("two_factor_required").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("challenge").GetString()));
        Assert.False(body.TryGetProperty("access_token", out _));
    }

    [Fact]
    public async Task Verifying_the_second_factor_returns_a_token_that_satisfies_the_2fa_gate()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Record.Sign], withTwoFactor: true, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        var challenge = (await login.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("challenge").GetString();

        var code = new TotpService().GenerateCode(staff.TotpSecret!, DateTimeOffset.UtcNow);

        var verify = await client.PostAsJsonAsync(
            "/auth/2fa/verify", new { challenge, code }, Token);

        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var tokens = await verify.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(tokens.GetProperty("two_factor_satisfied").GetBoolean());

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.GetProperty("access_token").GetString());

        // record:sign is a medical scope, so this only succeeds because 2FA was satisfied.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_probe/sensitive", Token)).StatusCode);
    }

    [Fact]
    public async Task A_wrong_second_factor_code_is_refused()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Record.Sign], withTwoFactor: true, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        var challenge = (await login.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("challenge").GetString();

        var verify = await client.PostAsJsonAsync(
            "/auth/2fa/verify", new { challenge, code = "000000" }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, verify.StatusCode);
    }

    [Fact]
    public async Task A_challenge_cannot_be_redeemed_twice()
    {
        // Rotating on redemption is what spends it. A replayable challenge would be a bearer
        // credential that outlived its single use.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Record.Sign], withTwoFactor: true, cancellationToken: Token);
        using var client = Client(fixture);

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        var challenge = (await login.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("challenge").GetString();

        var code = new TotpService().GenerateCode(staff.TotpSecret!, DateTimeOffset.UtcNow);

        var first = await client.PostAsJsonAsync("/auth/2fa/verify", new { challenge, code }, Token);
        var second = await client.PostAsJsonAsync("/auth/2fa/verify", new { challenge, code }, Token);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task A_challenge_cannot_be_refreshed_into_a_session_without_the_second_factor()
    {
        // The challenge is the half-signed-in session's refresh token. Presenting it to
        // /auth/refresh must not do what only /auth/2fa/verify may: turn a password into a token.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: true, cancellationToken: Token);
        using var client = fixture.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        var challenge = (await login.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("challenge").GetString();

        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refresh.Headers.Add("Cookie", $"hp_refresh={challenge}");

        var refused = await client.SendAsync(refresh, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);

        // Refusing does not burn the challenge: the person who knows the code can still finish.
        var code = new TotpService().GenerateCode(staff.TotpSecret!, DateTimeOffset.UtcNow);
        var verify = await client.PostAsJsonAsync("/auth/2fa/verify", new { challenge, code }, Token);

        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_account_are_indistinguishable()
    {
        // Different answers here would turn the login form into an account-enumeration oracle.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, cancellationToken: Token);
        using var client = Client(fixture);

        var wrongPassword = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = "not-the-password" }, Token);

        var unknownAccount = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = "nobody@happypaws.test", password = "not-the-password" }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownAccount.StatusCode);

        var a = (await wrongPassword.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        var b = (await unknownAccount.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal(a.GetProperty("code").GetString(), b.GetProperty("code").GetString());
        Assert.Equal(a.GetProperty("message").GetString(), b.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_malformed_login_is_rejected_by_validation_not_by_a_credential_check()
    {
        using var client = Client(fixture);

        var response = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = "not-an-email", password = "x" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(ErrorCodes.ValidationFailed, error.GetProperty("code").GetString());
    }
}
