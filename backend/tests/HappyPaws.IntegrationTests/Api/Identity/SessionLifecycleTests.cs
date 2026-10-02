using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Identity;

/// <summary>
/// Refresh rotation, reuse detection, device listing and revocation.
/// </summary>
[Collection(nameof(ApiCollection))]
public class SessionLifecycleTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, SeededStaff Staff, string AccessToken)> SignInAsync(
        IReadOnlyCollection<string>? scopes = null)
    {
        var staff = await IdentitySeed.StaffAsync(
            fixture, scopes ?? [Scopes.Queue.Read], withTwoFactor: false, cancellationToken: Token);

        var client = fixture.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);

        var accessToken = (await login.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("access_token").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return (client, staff, accessToken);
    }

    [Fact]
    public async Task Refresh_issues_a_new_access_token_and_rotates_the_cookie()
    {
        var (client, staff, firstAccessToken) = await SignInAsync();
        using var _ = client;

        var sessionsBefore = await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token);
        var hashBefore = sessionsBefore.Single().RefreshTokenHash;

        var refresh = await client.PostAsync("/auth/refresh", content: null, Token);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var secondAccessToken = (await refresh.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("access_token").GetString();
        Assert.NotEqual(firstAccessToken, secondAccessToken);

        var sessionsAfter = await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token);

        // Same session, new refresh token — "rotate refresh token ทุกครั้ง".
        Assert.Single(sessionsAfter);
        Assert.NotEqual(hashBefore, sessionsAfter.Single().RefreshTokenHash);
    }

    [Fact]
    public async Task Replaying_a_rotated_refresh_token_kills_the_whole_session()
    {
        // Replay means two parties hold the token — the real device and whoever copied it. There
        // is no way to tell which is calling, so the session dies and both must sign in again.
        var (client, staff, _) = await SignInAsync();
        using var _unused = client;

        var cookieBefore = ExtractRefreshCookie(await client.PostAsync("/auth/refresh", null, Token));

        // Rotate again so the captured value is now stale.
        await client.PostAsync("/auth/refresh", null, Token);

        using var attacker = fixture.CreateClient();
        using var replay = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        replay.Headers.Add("Cookie", $"hp_refresh={cookieBefore}");

        var response = await attacker.SendAsync(replay, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // And the legitimate device is locked out too — that is the intended trade.
        var later = await client.PostAsync("/auth/refresh", null, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, later.StatusCode);

        var sessions = await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token);
        Assert.All(sessions, session => Assert.NotNull(session.RevokedAt));
    }

    [Fact]
    public async Task Sessions_are_listed_with_the_current_one_flagged()
    {
        var (client, staff, _) = await SignInAsync();
        using var _unused = client;

        var sessions = await client.GetFromJsonAsync<JsonElement>("/me/sessions", Token);
        var rows = sessions.EnumerateArray().ToArray();

        Assert.Single(rows);
        Assert.True(rows[0].GetProperty("current").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(rows[0].GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Revoking_another_account_s_session_reports_404_not_403()
    {
        // Session ids are opaque. Confirming one exists would let a caller probe for other
        // people's devices.
        var (alice, _, _) = await SignInAsync();
        using var _a = alice;

        var (bob, bobStaff, _) = await SignInAsync();
        using var _b = bob;

        var bobSessionId = (await IdentitySeed.SessionsAsync(fixture, bobStaff.UserId, Token)).Single().Id;

        var response = await alice.DeleteAsync($"/me/sessions/{bobSessionId}", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Revoking_own_session_stops_its_refresh_token_working()
    {
        var (client, staff, _) = await SignInAsync();
        using var _unused = client;

        var sessionId = (await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token)).Single().Id;

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/me/sessions/{sessionId}", Token)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsync("/auth/refresh", null, Token)).StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_session_and_clears_the_cookie()
    {
        var (client, _, _) = await SignInAsync();
        using var _unused = client;

        var response = await client.PostAsJsonAsync("/auth/logout", new { allDevices = false }, Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsync("/auth/refresh", null, Token)).StatusCode);
    }

    [Fact]
    public async Task Logout_is_idempotent()
    {
        // A second click, an expired cookie and an already-ended session must all succeed quietly:
        // reporting "no such session" tells a holder of a stale token whether it was ever real.
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/logout", new { allDevices = false }, Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .First(value => value.StartsWith("hp_refresh=", StringComparison.Ordinal));

        return header["hp_refresh=".Length..header.IndexOf(';', StringComparison.Ordinal)];
    }
}
