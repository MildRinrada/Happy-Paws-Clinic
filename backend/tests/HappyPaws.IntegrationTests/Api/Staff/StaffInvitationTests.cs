using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// An invitation is how a stranger becomes a member of staff, so its link is tested as the
/// credential it is: single-use, time-boxed, never returned to anyone but the invitee.
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffInvitationTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private async Task<(SeededClinic Clinic, HttpClient Manager, string RoleName)> ArrangeAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Patient.Read);
        var (_, roleName) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read, Scopes.Patient.Read], Token);

        return (clinic, manager, roleName);
    }

    [Fact]
    public async Task An_invited_person_sets_a_password_and_a_second_factor_then_signs_in()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail(), [roleName], [clinic.BranchId], Token);

        using var session = await FreshInstance.SignInAsync(fixture, member.Email, member.Password, member.TotpSecret, Token);

        var me = await session.GetFromJsonAsync<JsonElement>("/me", Token);
        var membership = me.GetProperty("memberships").EnumerateArray().Single();

        Assert.Equal(member.StaffId, membership.GetProperty("staff_id").GetString());
        Assert.Equal("veterinarian", membership.GetProperty("staff_type").GetString());
        Assert.Equal("ส.พ. 06120", membership.GetProperty("license_no").GetString());
        Assert.Equal([roleName], membership.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        Assert.Equal([clinic.BranchId], membership.GetProperty("branches").EnumerateArray().Select(b => b.GetString()));

        // "ผู้ถูกเชิญตั้งรหัสผ่านและ 2FA" — both, and the second is really on the account.
        Assert.True(me.GetProperty("two_factor").GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task The_link_is_sent_to_the_invitee_and_never_returned_to_the_inviter()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        var response = await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(Token);
        var link = fixture.Invitations.LatestTo(email);

        // Whoever invites could otherwise accept on the invitee's behalf and sign records as them.
        Assert.DoesNotContain(link.Token, raw);
        Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);

        var invited = await BodyAsync(response);

        Assert.Equal("invited", invited.GetProperty("status").GetString());
        Assert.Equal(link.InvitationId, invited.GetProperty("id").GetString());
        Assert.InRange(
            invited.GetProperty("invitation_expires_at").GetDateTimeOffset(),
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(1));
    }

    [Fact]
    public async Task Only_the_hash_of_the_link_is_stored()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        (await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token)).EnsureSuccessStatusCode();

        var link = fixture.Invitations.LatestTo(email);

        using var scope = fixture.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>()
            .Set<StaffInvitation>().AsNoTracking()
            .SingleAsync(invitation => invitation.Id == link.InvitationId, Token);

        // A leaked backup must not hand anyone a working invitation to become a vet.
        Assert.NotEqual(link.Token, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Nothing_the_invitee_can_use_exists_until_they_accept()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        (await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token)).EnsureSuccessStatusCode();

        using var anonymous = fixture.CreateClient();

        var login = await anonymous.PostAsJsonAsync(
            "/auth/staff/login", new { email, password = StaffScenario.Password }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Asking_for_the_setup_twice_shows_the_same_secret()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        (await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token)).EnsureSuccessStatusCode();

        var link = fixture.Invitations.LatestTo(email);
        var path = $"/staff/members/invitations/{link.InvitationId}/accept";

        using var invitee = fixture.CreateClient();

        var first = await BodyAsync(await invitee.PostAsJsonAsync(path, new { token = link.Token }, Token));
        var second = await BodyAsync(await invitee.PostAsJsonAsync(path, new { token = link.Token }, Token));

        Assert.Equal("two_factor_setup_required", first.GetProperty("status").GetString());

        // A page refresh must not invalidate the QR code already scanned.
        Assert.Equal(
            first.GetProperty("two_factor").GetProperty("secret").GetString(),
            second.GetProperty("two_factor").GetProperty("secret").GetString());

        Assert.StartsWith("otpauth://totp/", first.GetProperty("two_factor").GetProperty("otp_auth_uri").GetString());
    }

    [Fact]
    public async Task A_wrong_code_creates_nothing_and_the_link_still_works()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        (await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token)).EnsureSuccessStatusCode();

        var link = fixture.Invitations.LatestTo(email);
        var path = $"/staff/members/invitations/{link.InvitationId}/accept";

        using var invitee = fixture.CreateClient();

        var setup = await BodyAsync(await invitee.PostAsJsonAsync(path, new { token = link.Token }, Token));
        var secret = setup.GetProperty("two_factor").GetProperty("secret").GetString()!;

        var wrong = await invitee.PostAsJsonAsync(
            path, new { token = link.Token, password = StaffScenario.Password, code = "000000" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
        Assert.Equal("code", (await BodyAsync(wrong)).GetProperty("error").GetProperty("field").GetString());

        // The second factor is only on an account once it has been shown to work, so a mistyped
        // code costs the invitee nothing.
        var right = await invitee.PostAsJsonAsync(
            path,
            new
            {
                token = link.Token,
                password = StaffScenario.Password,
                code = new TotpService().GenerateCode(secret, DateTimeOffset.UtcNow),
            },
            Token);

        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        Assert.Equal("accepted", (await BodyAsync(right)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_link_cannot_be_used_twice()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail(), [roleName], [clinic.BranchId], Token);

        var link = fixture.Invitations.LatestTo(member.Email);

        using var invitee = fixture.CreateClient();

        var again = await invitee.PostAsJsonAsync(
            $"/staff/members/invitations/{link.InvitationId}/accept", new { token = link.Token }, Token);

        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task A_wrong_token_an_unknown_id_and_an_expired_link_all_answer_the_same()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();
        (await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token)).EnsureSuccessStatusCode();

        var link = fixture.Invitations.LatestTo(email);

        using var invitee = fixture.CreateClient();

        var wrongToken = await invitee.PostAsJsonAsync(
            $"/staff/members/invitations/{link.InvitationId}/accept", new { token = "not-the-token" }, Token);

        var unknownId = await invitee.PostAsJsonAsync(
            "/staff/members/invitations/sin_does_not_exist/accept", new { token = link.Token }, Token);

        // Seven days on, by moving the row rather than the clock.
        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>()
                .Set<StaffInvitation>()
                .Where(invitation => invitation.Id == link.InvitationId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(invitation => invitation.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)),
                    Token);
        }

        var expired = await invitee.PostAsJsonAsync(
            $"/staff/members/invitations/{link.InvitationId}/accept", new { token = link.Token }, Token);

        // An anonymous caller learns nothing about which invitations exist or why one failed.
        Assert.Equal(HttpStatusCode.NotFound, wrongToken.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);

        Assert.Equal(
            (await BodyAsync(wrongToken)).GetProperty("error").GetProperty("message").GetString(),
            (await BodyAsync(expired)).GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public async Task Inviting_again_kills_the_old_link_and_keeps_one_invitation()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var email = StaffScenario.NewEmail();

        var first = await BodyAsync(await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token));
        var oldLink = fixture.Invitations.LatestTo(email);

        var second = await BodyAsync(await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token));
        var newLink = fixture.Invitations.LatestTo(email);

        // One row to answer "have we invited them", refreshed in place.
        Assert.Equal(first.GetProperty("id").GetString(), second.GetProperty("id").GetString());
        Assert.NotEqual(oldLink.Token, newLink.Token);

        using var invitee = fixture.CreateClient();
        var path = $"/staff/members/invitations/{newLink.InvitationId}/accept";

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await invitee.PostAsJsonAsync(path, new { token = oldLink.Token }, Token)).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await invitee.PostAsJsonAsync(path, new { token = newLink.Token }, Token)).StatusCode);
    }

    [Fact]
    public async Task An_address_that_already_signs_in_here_is_not_invited_again()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail(), [roleName], [clinic.BranchId], Token);

        var again = await StaffScenario.InviteAsync(manager, member.Email, [roleName], [clinic.BranchId], Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(
            "staff_already_exists",
            (await BodyAsync(again)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Nobody_invites_someone_into_a_role_that_holds_more_than_they_do()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var senior = fixture.Manager(clinic, Scopes.Record.Sign);
        using var junior = fixture.Manager(clinic, Scopes.Queue.Read);

        var (_, signingRole) = await StaffScenario.CreateRoleAsync(senior, [Scopes.Record.Sign], Token);

        var response = await StaffScenario.InviteAsync(
            junior, StaffScenario.NewEmail(), [signingRole], [clinic.BranchId], Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "insufficient_scope",
            (await BodyAsync(response)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_role_or_a_branch_that_does_not_exist_is_refused()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var unknownRole = await StaffScenario.InviteAsync(
            manager, StaffScenario.NewEmail(), ["role-that-is-not-there"], [clinic.BranchId], Token);

        var unknownBranch = await StaffScenario.InviteAsync(
            manager, StaffScenario.NewEmail(), [roleName], ["brn_that_is_not_there"], Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownRole.StatusCode);
        Assert.Equal("roles", (await BodyAsync(unknownRole)).GetProperty("error").GetProperty("field").GetString());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownBranch.StatusCode);
        Assert.Equal("branches", (await BodyAsync(unknownBranch)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_branch_bound_manager_cannot_place_someone_at_a_branch_they_do_not_work_at()
    {
        var (clinic, manager, roleName) = await ArrangeAsync();
        using var _ = manager;

        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var response = await StaffScenario.InviteAsync(
            manager, StaffScenario.NewEmail(), [roleName], [elsewhere.BranchId], Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Nor leave the list empty, which a token reads as "not branch-bound at all".
        var everywhere = await StaffScenario.InviteAsync(manager, StaffScenario.NewEmail(), [roleName], [], Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, everywhere.StatusCode);
    }

    [Fact]
    public async Task Inviting_requires_staff_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var reader = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Staff.Read], [clinic.BranchId]));

        var response = await StaffScenario.InviteAsync(
            reader, StaffScenario.NewEmail(), ["anything"], [clinic.BranchId], Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
