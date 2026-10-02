using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Identity;

/// <summary>
/// <c>PATCH /me</c>, <c>POST /me/2fa/enroll</c> and <c>PATCH /staff/users/{id}/status</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AccountSelfServiceTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static string Code(string secret) => new TotpService().GenerateCode(secret, DateTimeOffset.UtcNow);

    private async Task<SeededStaff> StaffAsync(params string[] scopes) =>
        await IdentitySeed.StaffAsync(fixture, scopes.Length == 0 ? [Scopes.Queue.Read] : scopes, withTwoFactor: false, cancellationToken: Token);

    private HttpClient As(SeededStaff staff, bool twoFactorSatisfied = false, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, staff.UserId, staff.StaffId, scopes, ["brn_sukhumvit39"], twoFactorSatisfied));

    /// <summary>Password then second factor, as a browser would; returns the response of the second step.</summary>
    private async Task<HttpResponseMessage> SignInWithAsync(SeededStaff staff, string code)
    {
        using var client = fixture.CreateClient();

        var login = await client.PostAsJsonAsync("/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        login.EnsureSuccessStatusCode();
        var challenge = (await BodyAsync(login)).GetProperty("challenge").GetString();

        return await client.PostAsJsonAsync("/auth/2fa/verify", new { challenge, code }, Token);
    }

    // ---- PATCH /me ---------------------------------------------------------------------------

    [Fact]
    public async Task Name_language_and_notification_choices_change_without_a_password()
    {
        var staff = await StaffAsync();
        using var client = As(staff);

        var response = await client.PatchAsJsonAsync("/me", new
        {
            display_name = "สพ.ญ. มินท์ (ชื่อใหม่)",
            language = "en",
            notifications = new { sms = false },
        }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await BodyAsync(response);

        Assert.Equal("สพ.ญ. มินท์ (ชื่อใหม่)", me.GetProperty("display_name").GetString());
        Assert.Equal("en", me.GetProperty("language").GetString());
        Assert.False(me.GetProperty("notifications").GetProperty("sms").GetBoolean());
        Assert.True(me.GetProperty("notifications").GetProperty("email").GetBoolean());
        Assert.Equal(staff.Email, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task A_new_email_needs_the_current_password()
    {
        var staff = await StaffAsync();
        using var client = As(staff);
        var email = $"new-{Guid.NewGuid():N}@happypaws.test";

        var without = await client.PatchAsJsonAsync("/me", new { email }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, without.StatusCode);
        Assert.Equal("current_password", (await BodyAsync(without)).GetProperty("error").GetProperty("field").GetString());

        var wrong = await client.PatchAsJsonAsync("/me", new { email, current_password = "not-it" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);

        var right = await client.PatchAsJsonAsync("/me", new { email, current_password = staff.Password }, Token);
        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        Assert.Equal(email, (await BodyAsync(right)).GetProperty("email").GetString());

        // And it is the address that signs in from now on.
        using var browser = fixture.CreateClient();
        var login = await browser.PostAsJsonAsync("/auth/staff/login", new { email, password = staff.Password }, Token);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task An_email_another_account_uses_is_refused()
    {
        var staff = await StaffAsync();
        var other = await StaffAsync();
        using var client = As(staff);

        var response = await client.PatchAsJsonAsync("/me", new { email = other.Email, current_password = staff.Password }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("contact_taken", (await BodyAsync(response)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_stale_version_is_refused()
    {
        var staff = await StaffAsync();
        using var client = As(staff);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/me")
        {
            Content = JsonContent.Create(new { language = "en" }),
        };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(request, Token)).StatusCode);
    }

    // ---- POST /me/2fa/enroll -----------------------------------------------------------------

    [Fact]
    public async Task Enrolling_takes_a_password_then_a_code_and_returns_recovery_codes_once()
    {
        var staff = await StaffAsync();
        using var client = As(staff);

        var started = await client.PostAsJsonAsync("/me/2fa/enroll", new { current_password = staff.Password }, Token);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Contains("no-store", started.Headers.CacheControl?.ToString());

        var setup = await BodyAsync(started);
        Assert.Equal("confirmation_required", setup.GetProperty("status").GetString());
        var secret = setup.GetProperty("secret").GetString()!;
        Assert.StartsWith("otpauth://", setup.GetProperty("otp_auth_uri").GetString());

        // Not on until proven: /me still says off.
        var before = await client.GetFromJsonAsync<JsonElement>("/me", Token);
        Assert.False(before.GetProperty("two_factor").GetProperty("enabled").GetBoolean());

        var confirmed = await client.PostAsJsonAsync("/me/2fa/enroll", new { code = Code(secret) }, Token);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);

        var done = await BodyAsync(confirmed);
        Assert.Equal("enabled", done.GetProperty("status").GetString());
        Assert.Equal(8, done.GetProperty("recovery_codes").GetArrayLength());
        Assert.False(done.TryGetProperty("secret", out _));

        var after = await client.GetFromJsonAsync<JsonElement>("/me", Token);
        Assert.True(after.GetProperty("two_factor").GetProperty("enabled").GetBoolean());
        Assert.Equal(8, after.GetProperty("two_factor").GetProperty("recovery_codes_left").GetInt32());

        // The next sign-in asks for the code it was set up with.
        Assert.Equal(HttpStatusCode.OK, (await SignInWithAsync(staff, Code(secret))).StatusCode);
    }

    [Fact]
    public async Task A_recovery_code_signs_in_once()
    {
        var staff = await StaffAsync();
        using var client = As(staff);

        var secret = (await BodyAsync(await client.PostAsJsonAsync(
            "/me/2fa/enroll", new { current_password = staff.Password }, Token))).GetProperty("secret").GetString()!;
        var recovery = (await BodyAsync(await client.PostAsJsonAsync(
            "/me/2fa/enroll", new { code = Code(secret) }, Token))).GetProperty("recovery_codes")[0].GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await SignInWithAsync(staff, recovery.ToUpperInvariant())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInWithAsync(staff, recovery)).StatusCode);
    }

    [Fact]
    public async Task Enrolling_from_a_real_session_keeps_that_session_alive()
    {
        var staff = await StaffAsync();
        using var browser = fixture.CreateClient();

        var login = await browser.PostAsJsonAsync("/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", (await BodyAsync(login)).GetProperty("access_token").GetString());

        var secret = (await BodyAsync(await browser.PostAsJsonAsync(
            "/me/2fa/enroll", new { current_password = staff.Password }, Token))).GetProperty("secret").GetString()!;
        (await browser.PostAsJsonAsync("/me/2fa/enroll", new { code = Code(secret) }, Token)).EnsureSuccessStatusCode();

        // An account with 2FA cannot refresh a session that never passed it; this one just did.
        var refreshed = await browser.PostAsync("/auth/refresh", null, Token);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.True((await BodyAsync(refreshed)).GetProperty("two_factor_satisfied").GetBoolean());
    }

    [Fact]
    public async Task Enrolling_refuses_a_wrong_password_a_wrong_code_and_a_confirmation_out_of_order()
    {
        var staff = await StaffAsync();
        using var client = As(staff);

        var outOfOrder = await client.PostAsJsonAsync("/me/2fa/enroll", new { code = "123456" }, Token);
        Assert.Equal(HttpStatusCode.Conflict, outOfOrder.StatusCode);

        var wrongPassword = await client.PostAsJsonAsync("/me/2fa/enroll", new { current_password = "not-it" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongPassword.StatusCode);

        (await client.PostAsJsonAsync("/me/2fa/enroll", new { current_password = staff.Password }, Token)).EnsureSuccessStatusCode();
        var wrongCode = await client.PostAsJsonAsync("/me/2fa/enroll", new { code = "000000" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCode.StatusCode);
    }

    [Fact]
    public async Task Replacing_a_second_factor_needs_a_session_that_passed_the_current_one()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: true, cancellationToken: Token);
        using var unproven = As(staff, twoFactorSatisfied: false);

        var response = await unproven.PostAsJsonAsync("/me/2fa/enroll", new { current_password = staff.Password }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- PATCH /staff/users/{id}/status ------------------------------------------------------

    [Fact]
    public async Task A_suspended_account_cannot_sign_in_until_it_is_reactivated()
    {
        var manager = await StaffAsync(Scopes.User.Manage);
        var target = await StaffAsync();
        using var client = As(manager, twoFactorSatisfied: true, Scopes.User.Manage, Scopes.Queue.Read);

        var suspended = await client.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "suspended" }, Token);
        Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        Assert.Equal("suspended", (await BodyAsync(suspended)).GetProperty("status").GetString());

        using var browser = fixture.CreateClient();
        var refused = await browser.PostAsJsonAsync("/auth/staff/login", new { email = target.Email, password = target.Password }, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);

        (await client.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "active" }, Token)).EnsureSuccessStatusCode();
        var allowed = await browser.PostAsJsonAsync("/auth/staff/login", new { email = target.Email, password = target.Password }, Token);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task A_deactivated_account_is_not_reopened_by_a_status_change()
    {
        var manager = await StaffAsync(Scopes.User.Manage);
        var target = await StaffAsync();
        using var client = As(manager, twoFactorSatisfied: true, Scopes.User.Manage, Scopes.Queue.Read);

        (await client.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "deactivated" }, Token)).EnsureSuccessStatusCode();
        var reopen = await client.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "active" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, reopen.StatusCode);
    }

    [Fact]
    public async Task Nobody_changes_their_own_status()
    {
        var manager = await StaffAsync(Scopes.User.Manage);
        using var client = As(manager, twoFactorSatisfied: true, Scopes.User.Manage, Scopes.Queue.Read);

        var response = await client.PatchAsJsonAsync($"/staff/users/{manager.UserId}/status", new { status = "suspended" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Changing_status_needs_user_manage_and_a_second_factor_and_a_known_status()
    {
        var manager = await StaffAsync(Scopes.User.Manage);
        var target = await StaffAsync();

        using var noScope = As(manager, twoFactorSatisfied: true, Scopes.Queue.Read);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await noScope.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "suspended" }, Token)).StatusCode);

        using var noSecondFactor = As(manager, twoFactorSatisfied: false, Scopes.User.Manage, Scopes.Queue.Read);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await noSecondFactor.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "suspended" }, Token)).StatusCode);

        using var client = As(manager, twoFactorSatisfied: true, Scopes.User.Manage, Scopes.Queue.Read);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await client.PatchAsJsonAsync($"/staff/users/{target.UserId}/status", new { status = "on-holiday" }, Token)).StatusCode);
    }

    [Fact]
    public async Task Someone_holding_more_than_the_caller_cannot_be_suspended()
    {
        var manager = await StaffAsync(Scopes.User.Manage);
        var senior = await StaffAsync(Scopes.User.Manage, Scopes.Role.Manage);
        using var client = As(manager, twoFactorSatisfied: true, Scopes.User.Manage, Scopes.Queue.Read);

        var response = await client.PatchAsJsonAsync($"/staff/users/{senior.UserId}/status", new { status = "suspended" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
