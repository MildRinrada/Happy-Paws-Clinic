using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// The staff list, and changing or ending someone's place on it.
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffMembersTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static IEnumerable<string?> Ids(JsonElement page) =>
        page.GetProperty("data").EnumerateArray().Select(member => member.GetProperty("id").GetString());

    /// <summary>A clinic, a manager at it, a role the manager may hand out, and one accepted member.</summary>
    private async Task<(SeededClinic Clinic, HttpClient Manager, string RoleName, AcceptedMember Member)> ArrangeAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Patient.Read, Scopes.Appointment.Read);
        var (_, roleName) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read], Token);

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail(), [roleName], [clinic.BranchId], Token);

        return (clinic, manager, roleName, member);
    }

    [Fact]
    public async Task The_list_shows_who_works_at_a_branch_in_the_shape_me_uses()
    {
        var (clinic, manager, roleName, member) = await ArrangeAsync();
        using var _ = manager;

        var page = await manager.GetFromJsonAsync<JsonElement>($"/staff/members?branch={clinic.BranchId}", Token);
        var listed = page.GetProperty("data").EnumerateArray()
            .Single(entry => entry.GetProperty("id").GetString() == member.StaffId);

        Assert.Equal(member.Email, listed.GetProperty("email").GetString());
        Assert.Equal("active", listed.GetProperty("status").GetString());
        Assert.Equal([roleName], listed.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        Assert.Equal([clinic.BranchId], listed.GetProperty("branches").EnumerateArray().Select(b => b.GetString()));
        Assert.True(listed.GetProperty("two_factor_enabled").GetBoolean());

        // The contract's list envelope, from a page that was returned without a wrapper.
        Assert.True(page.TryGetProperty("page", out var _));
    }

    [Fact]
    public async Task An_open_invitation_is_listed_under_status_invited_and_nowhere_else()
    {
        var (clinic, manager, roleName, _) = await ArrangeAsync();
        using var __ = manager;

        var email = StaffScenario.NewEmail();
        var invited = await BodyAsync(await StaffScenario.InviteAsync(manager, email, [roleName], [clinic.BranchId], Token));
        var invitationId = invited.GetProperty("id").GetString();

        var pending = await manager.GetFromJsonAsync<JsonElement>(
            $"/staff/members?status=invited&branch={clinic.BranchId}", Token);

        var employed = await manager.GetFromJsonAsync<JsonElement>(
            $"/staff/members?branch={clinic.BranchId}", Token);

        // The contract lets a clinic invite and has no other endpoint that says who is outstanding.
        Assert.Contains(invitationId, Ids(pending));
        Assert.DoesNotContain(invitationId, Ids(employed));
    }

    [Fact]
    public async Task A_branch_bound_reader_does_not_see_another_branchs_staff()
    {
        var (clinic, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var outsider = fixture.Manager(elsewhere);

        var page = await outsider.GetFromJsonAsync<JsonElement>("/staff/members?limit=100", Token);

        Assert.DoesNotContain(member.StaffId, Ids(page));

        // And asking for that branch outright is refused rather than answered with an empty page.
        var response = await outsider.GetAsync($"/staff/members?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_status_is_a_validation_error_not_an_empty_list()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic);

        var response = await manager.GetAsync("/staff/members?status=on-holiday", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_members_position_and_licence_can_be_changed_without_touching_the_rest()
    {
        var (clinic, manager, roleName, member) = await ArrangeAsync();
        using var _ = manager;

        var response = await manager.PatchAsJsonAsync(
            $"/staff/members/{member.StaffId}", new { staff_type = "senior_veterinarian", license_no = "ส.พ. 09999" }, Token);

        response.EnsureSuccessStatusCode();

        var updated = await BodyAsync(response);

        Assert.Equal("senior_veterinarian", updated.GetProperty("staff_type").GetString());
        Assert.Equal("ส.พ. 09999", updated.GetProperty("license_no").GetString());

        // PATCH: what was not sent is left alone.
        Assert.Equal([roleName], updated.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        Assert.Equal([clinic.BranchId], updated.GetProperty("branches").EnumerateArray().Select(b => b.GetString()));
    }

    [Fact]
    public async Task Changing_a_members_roles_changes_what_their_next_token_can_do()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        var (_, widerRole) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read, Scopes.Appointment.Read], Token);

        (await manager.PatchAsJsonAsync(
            $"/staff/members/{member.StaffId}", new { roles = new[] { widerRole } }, Token)).EnsureSuccessStatusCode();

        using var session = await FreshInstance.SignInAsync(fixture, member.Email, member.Password, member.TotpSecret, Token);

        var capabilities = await session.GetFromJsonAsync<JsonElement>("/me/capabilities", Token);

        Assert.Contains(
            Scopes.Appointment.Read,
            capabilities.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Fact]
    public async Task Nobody_promotes_a_member_into_a_role_that_holds_more_than_they_do()
    {
        var (clinic, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        using var senior = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Record.Sign);
        var (_, signingRole) = await StaffScenario.CreateRoleAsync(senior, [Scopes.Record.Sign], Token);

        var response = await manager.PatchAsJsonAsync(
            $"/staff/members/{member.StaffId}", new { roles = new[] { signingRole } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Nobody_changes_a_member_who_holds_more_than_they_do()
    {
        // The other direction. Without it, demoting or deactivating everyone above you is a way up.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var senior = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Record.Sign);
        using var junior = fixture.Manager(clinic, Scopes.Queue.Read);

        var (_, signingRole) = await StaffScenario.CreateRoleAsync(senior, [Scopes.Record.Sign], Token);
        var (_, plainRole) = await StaffScenario.CreateRoleAsync(junior, [Scopes.Queue.Read], Token);

        var vet = await StaffScenario.InviteAndAcceptAsync(
            fixture, senior, StaffScenario.NewEmail(), [signingRole], [clinic.BranchId], Token);

        var demote = await junior.PatchAsJsonAsync(
            $"/staff/members/{vet.StaffId}", new { roles = new[] { plainRole } }, Token);

        var deactivate = await junior.PostAsJsonAsync($"/staff/members/{vet.StaffId}/deactivate", new { }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, demote.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
    }

    [Fact]
    public async Task A_stale_version_is_refused()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/staff/members/{member.StaffId}")
        {
            Content = JsonContent.Create(new { staff_type = "nurse" }),
        };

        request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        var response = await manager.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("version_conflict", (await BodyAsync(response)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_member_at_another_branch_is_not_found_by_a_branch_bound_manager()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var outsider = fixture.Manager(elsewhere, Scopes.Queue.Read);

        var response = await outsider.PatchAsJsonAsync(
            $"/staff/members/{member.StaffId}", new { staff_type = "nurse" }, Token);

        // 404, not 403: the same answer as an id that does not exist.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deactivating_ends_every_session_and_refuses_the_next_sign_in()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        using var session = await FreshInstance.SignInAsync(fixture, member.Email, member.Password, member.TotpSecret, Token);

        var response = await manager.PostAsJsonAsync($"/staff/members/{member.StaffId}/deactivate", new { }, Token);
        response.EnsureSuccessStatusCode();

        Assert.Equal("deactivated", (await BodyAsync(response)).GetProperty("status").GetString());

        // "ปิดสิทธิ์ทันที": no new token can be minted, by refresh or by signing in again.
        Assert.Equal(HttpStatusCode.Unauthorized, (await session.PostAsync("/auth/refresh", null, Token)).StatusCode);

        using var anonymous = fixture.CreateClient();
        var login = await anonymous.PostAsJsonAsync(
            "/auth/staff/login", new { email = member.Email, password = member.Password }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task A_deactivated_member_stays_on_record()
    {
        var (clinic, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        (await manager.PostAsJsonAsync($"/staff/members/{member.StaffId}/deactivate", new { }, Token))
            .EnsureSuccessStatusCode();

        var former = await manager.GetFromJsonAsync<JsonElement>(
            $"/staff/members?status=deactivated&branch={clinic.BranchId}", Token);

        var working = await manager.GetFromJsonAsync<JsonElement>(
            $"/staff/members?status=active&branch={clinic.BranchId}", Token);

        // "เก็บชื่อในเวชระเบียนเดิม": the row and the account stay, because records they signed
        // still point at them.
        Assert.Contains(member.StaffId, Ids(former));
        Assert.DoesNotContain(member.StaffId, Ids(working));

        var account = await IdentitySeed.ReloadAccountAsync(fixture, member.UserId, Token);

        Assert.NotNull(account);
    }

    [Fact]
    public async Task Deactivating_twice_succeeds_and_changes_nothing()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        var first = await manager.PostAsJsonAsync($"/staff/members/{member.StaffId}/deactivate", new { }, Token);
        var second = await manager.PostAsJsonAsync($"/staff/members/{member.StaffId}/deactivate", new { }, Token);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Staff_types_are_the_positions_in_use_plus_the_contracts_own()
    {
        var (_, manager, _, member) = await ArrangeAsync();
        using var _ = manager;

        (await manager.PatchAsJsonAsync(
            $"/staff/members/{member.StaffId}", new { staff_type = "groomer" }, Token)).EnsureSuccessStatusCode();

        var types = await manager.GetFromJsonAsync<JsonElement>("/staff/staff-types", Token);
        var entries = types.GetProperty("data").EnumerateArray().ToList();

        // Defined by being used: the contract calls them clinic-defined and has no endpoint that
        // defines one.
        var groomer = entries.Single(entry => entry.GetProperty("staff_type").GetString() == "groomer");
        Assert.True(groomer.GetProperty("members").GetInt32() >= 1);

        Assert.Contains(entries, entry => entry.GetProperty("staff_type").GetString() == "cashier");
    }

    [Theory]
    [InlineData("GET", "/staff/members", Scopes.Staff.Read)]
    [InlineData("GET", "/staff/staff-types", Scopes.Staff.Read)]
    [InlineData("PATCH", "/staff/members/stf_x", Scopes.Staff.Manage)]
    [InlineData("POST", "/staff/members/stf_x/deactivate", Scopes.Staff.Manage)]
    [InlineData("POST", "/staff/members/invitations", Scopes.Staff.Manage)]
    public async Task Every_staff_route_refuses_a_caller_without_its_scope(string method, string path, string required)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var underscoped = fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [.. StaffScenario.ManagerScopes.Where(scope => scope != required)],
            [clinic.BranchId]));

        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (method != "GET")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await underscoped.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
