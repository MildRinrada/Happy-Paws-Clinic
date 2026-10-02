using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// Roles are data a clinic edits, so the edits are where privilege could leak: a scope that does
/// not exist, a role that holds more than its author, a change that never reaches the people who
/// hold it.
/// </summary>
[Collection(nameof(ApiCollection))]
public class RoleManagementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Fact]
    public async Task The_catalogue_lists_every_scope_split_into_resource_and_action()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic);

        var catalogue = await manager.GetFromJsonAsync<JsonElement>("/staff/permissions", Token);
        var entries = catalogue.GetProperty("data").EnumerateArray().ToList();

        var sign = entries.Single(entry => entry.GetProperty("scope").GetString() == Scopes.Record.Sign);

        Assert.Equal("record", sign.GetProperty("resource").GetString());
        Assert.Equal("sign", sign.GetProperty("action").GetString());

        // The portal marker is not something a clinic hands out.
        Assert.DoesNotContain(entries, entry => entry.GetProperty("scope").GetString() == Scopes.Portal);
    }

    [Fact]
    public async Task A_created_role_is_listed_with_the_scopes_it_was_given()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Vitals.Write);

        var (id, name) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read, Scopes.Vitals.Write], Token);

        var roles = await manager.GetFromJsonAsync<JsonElement>("/staff/roles", Token);
        var created = roles.GetProperty("data").EnumerateArray().Single(role => role.GetProperty("id").GetString() == id);

        Assert.Equal(name, created.GetProperty("name").GetString());
        Assert.False(created.GetProperty("is_system").GetBoolean());
        Assert.Equal(
            [Scopes.Queue.Read, Scopes.Vitals.Write],
            created.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Fact]
    public async Task A_scope_that_does_not_exist_is_refused_and_named()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read);

        var response = await manager.PostAsJsonAsync(
            "/staff/roles",
            new { name = $"role-{Guid.NewGuid():N}", scopes = new[] { Scopes.Queue.Read, "queue:fly" } },
            Token);

        // "ตรวจ scope ที่ไม่มีจริง".
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = await ErrorAsync(response);

        Assert.Equal("scopes", error.GetProperty("field").GetString());
        Assert.Contains("queue:fly", error.GetProperty("details").GetProperty("errors").GetProperty("scopes")[0].GetString());
    }

    [Fact]
    public async Task A_role_name_already_in_use_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read);

        var (_, name) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read], Token);

        var again = await manager.PostAsJsonAsync(
            "/staff/roles", new { name, scopes = new[] { Scopes.Queue.Read } }, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("duplicate_role", (await ErrorAsync(again)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Nobody_writes_a_role_holding_more_than_they_do()
    {
        // role:manage is a permission to administer access. Without this rule it is quietly a
        // permission to have any access: write a role with record:sign in it, assign it to
        // yourself.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read);

        var response = await manager.PostAsJsonAsync(
            "/staff/roles",
            new { name = $"role-{Guid.NewGuid():N}", scopes = new[] { Scopes.Queue.Read, Scopes.Record.Sign } },
            Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = await ErrorAsync(response);

        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Equal(
            [Scopes.Record.Sign],
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("queue:*")]
    public async Task A_wildcard_is_granted_only_by_someone_who_holds_that_wildcard(string wildcard)
    {
        // Holding every queue scope one by one is not holding queue:* — the wildcard also covers
        // the queue scopes of a later release, which nobody can be said to hold yet.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call);

        var response = await manager.PostAsJsonAsync(
            "/staff/roles", new { name = $"role-{Guid.NewGuid():N}", scopes = new[] { wildcard } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Replacing_permissions_replaces_the_whole_set()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Patient.Read);

        var (id, _) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read, Scopes.Queue.Write], Token);

        var response = await manager.PutAsJsonAsync(
            $"/staff/roles/{id}/permissions", new { scopes = new[] { Scopes.Patient.Read } }, Token);

        response.EnsureSuccessStatusCode();

        var role = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        // "แทนที่ทั้งชุด" — not merged with what was there.
        Assert.Equal(
            [Scopes.Patient.Read],
            role.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Fact]
    public async Task A_stale_version_is_refused_rather_than_overwriting_someone_elses_edit()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Patient.Read);

        var (id, _) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read], Token);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/staff/roles/{id}/permissions")
        {
            Content = JsonContent.Create(new { scopes = new[] { Scopes.Patient.Read } }),
        };

        request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        var response = await manager.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("version_conflict", (await ErrorAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_caller_may_not_strip_scopes_they_do_not_hold_from_a_role()
    {
        // The other half of the grant rule. Taking record:sign out of a role is refused from
        // someone who does not hold it, for the same reason granting it is: otherwise demoting
        // everyone above you is a way up.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var senior = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Record.Sign);
        using var junior = fixture.Manager(clinic, Scopes.Queue.Read);

        var (id, _) = await StaffScenario.CreateRoleAsync(senior, [Scopes.Queue.Read, Scopes.Record.Sign], Token);

        var response = await junior.PutAsJsonAsync(
            $"/staff/roles/{id}/permissions", new { scopes = new[] { Scopes.Queue.Read } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_edited_role_reaches_its_holders_on_their_next_token()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Patient.Read);

        var (roleId, roleName) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read], Token);

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail(), [roleName], [clinic.BranchId], Token);

        using var session = await FreshInstance.SignInAsync(fixture, member.Email, member.Password, member.TotpSecret, Token);

        (await manager.PutAsJsonAsync(
            $"/staff/roles/{roleId}/permissions",
            new { scopes = new[] { Scopes.Queue.Read, Scopes.Patient.Read } },
            Token)).EnsureSuccessStatusCode();

        // Nobody was signed out. The scopes travel in the access token, so the change arrives
        // with the next one — which is what refresh mints.
        var refreshed = await session.PostAsync("/auth/refresh", content: null, Token);
        refreshed.EnsureSuccessStatusCode();

        session.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            (await refreshed.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("access_token").GetString());

        var capabilities = await session.GetFromJsonAsync<JsonElement>("/me/capabilities", Token);

        Assert.Contains(
            Scopes.Patient.Read,
            capabilities.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Theory]
    [InlineData("GET", "/staff/roles", Scopes.Role.Read)]
    [InlineData("POST", "/staff/roles", Scopes.Role.Manage)]
    [InlineData("PUT", "/staff/roles/rol_x/permissions", Scopes.Role.Manage)]
    [InlineData("GET", "/staff/permissions", Scopes.Role.Read)]
    public async Task Every_role_route_refuses_a_caller_without_its_scope(string method, string path, string required)
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
            request.Content = JsonContent.Create(new { name = "x", scopes = Array.Empty<string>() });
        }

        var response = await underscoped.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Managing_roles_requires_a_second_factor()
    {
        // role:manage is on the list of scopes that must be second-factor authenticated. A token
        // that holds it without 2FA is refused everywhere, not only on the role routes.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var withoutSecondFactor = fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            StaffScenario.ManagerScopes,
            [clinic.BranchId],
            twoFactorSatisfied: false));

        var response = await withoutSecondFactor.GetAsync("/staff/roles", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
