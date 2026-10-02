using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Infrastructure.Identity;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>Someone who was invited and accepted, with what they need to sign in.</summary>
public sealed record AcceptedMember(
    string Email,
    string Password,
    string TotpSecret,
    string StaffId,
    string UserId,
    string InvitationId);

/// <summary>
/// The staff-management requests the tests share: a manager's client, and the invite → set up →
/// accept path driven through the API.
/// </summary>
public static class StaffScenario
{
    public const string Password = "a-long-enough-password";

    /// <summary>What an administrator of staff and roles holds, for the tests that are not about scopes.</summary>
    public static readonly string[] ManagerScopes =
    [
        Scopes.Staff.Read, Scopes.Staff.Manage, Scopes.Role.Read, Scopes.Role.Manage,
    ];

    /// <summary>
    /// A manager's client. <paramref name="scopes"/> are in addition to the four management
    /// scopes, because a manager may only hand out what they hold themselves.
    /// </summary>
    public static HttpClient Manager(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [.. ManagerScopes, .. scopes],
            [clinic.BranchId]));

    public static string NewEmail(string prefix = "staff") => $"{prefix}-{Guid.NewGuid():N}@happypaws.test";

    /// <summary>Creates a role through the API and returns its name.</summary>
    public static async Task<(string Id, string Name)> CreateRoleAsync(
        HttpClient manager,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken)
    {
        var name = $"role-{Guid.NewGuid():N}";

        var response = await manager.PostAsJsonAsync("/staff/roles", new { name, scopes }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var role = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return (role.GetProperty("id").GetString()!, name);
    }

    public static Task<HttpResponseMessage> InviteAsync(
        HttpClient manager,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> branches,
        CancellationToken cancellationToken,
        string staffType = "veterinarian") =>
        manager.PostAsJsonAsync(
            "/staff/members/invitations",
            new
            {
                email,
                display_name = "สพ.ญ. ทดสอบ ระบบ",
                staff_type = staffType,
                roles,
                branches,
                license_no = "ส.พ. 06120",
            },
            cancellationToken);

    /// <summary>
    /// Invites, reads the link the invitee would have been emailed, sets up the second factor and
    /// accepts — the whole path, through the API.
    /// </summary>
    public static async Task<AcceptedMember> InviteAndAcceptAsync(
        ApiFixture fixture,
        HttpClient manager,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> branches,
        CancellationToken cancellationToken)
    {
        (await InviteAsync(manager, email, roles, branches, cancellationToken)).EnsureSuccessStatusCode();

        var link = fixture.Invitations.LatestTo(email);
        var path = $"/staff/members/invitations/{link.InvitationId}/accept";

        // No Authorization header: the invitee has no account yet. The token is the credential.
        using var invitee = fixture.CreateClient();

        var setup = await invitee.PostAsJsonAsync(path, new { token = link.Token }, cancellationToken);
        setup.EnsureSuccessStatusCode();

        var secret = (await setup.Content.ReadFromJsonAsync<JsonElement>(cancellationToken))
            .GetProperty("two_factor").GetProperty("secret").GetString()!;

        var accepted = await invitee.PostAsJsonAsync(
            path,
            new
            {
                token = link.Token,
                password = Password,
                code = new TotpService().GenerateCode(secret, DateTimeOffset.UtcNow),
            },
            cancellationToken);

        accepted.EnsureSuccessStatusCode();

        var body = await accepted.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return new AcceptedMember(
            email,
            Password,
            secret,
            body.GetProperty("staff_id").GetString()!,
            body.GetProperty("user_id").GetString()!,
            link.InvitationId);
    }
}
