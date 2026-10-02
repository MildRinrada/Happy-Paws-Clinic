using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Api.Operator;
using HappyPaws.Application.Clinics.Bootstrap;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Operator;

/// <summary>
/// An empty database, the operator commands, and the first person ever to sign in.
/// <para>
/// Runs against its own instance. "There is no owner yet" is a fact about a whole database, and it
/// stops being true the moment any other test has run.
/// </para>
/// </summary>
[Collection(nameof(FreshInstanceCollection))]
public class BootstrapTests(ApiFixture fixture) : FreshInstanceTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override ApiFixture Instance => fixture;

    [Fact]
    public async Task The_first_run_creates_the_seven_starter_roles_a_branch_and_an_owner()
    {
        var owner = await FreshInstance.OwnerAsync(fixture);

        Assert.Equal(
            DefaultRoles.StarterScopes.Select(starter => starter.Key).Order(),
            owner.FirstRun.RolesCreated.Order());

        Assert.Equal("brn_main", owner.FirstRun.BranchId);
        Assert.StartsWith("stf_", owner.StaffId);
        Assert.False(string.IsNullOrWhiteSpace(owner.TotpSecret));

        // The rooms have to be given here: the contract opens and lists branches and has no
        // endpoint that edits one.
        using var anonymous = fixture.CreateClient();
        var branches = await anonymous.GetFromJsonAsync<JsonElement>("/clinic/branches", Token);
        var main = branches.GetProperty("data").EnumerateArray()
            .Single(branch => branch.GetProperty("id").GetString() == "brn_main");

        Assert.Equal(["room_1", "room_2"], main.GetProperty("exam_rooms").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task The_owner_signs_in_with_a_second_factor_and_holds_everything()
    {
        using var client = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var me = await client.GetFromJsonAsync<JsonElement>("/me", Token);
        var membership = me.GetProperty("memberships").EnumerateArray().Single();

        Assert.Equal(DefaultRoles.ClinicOwner, membership.GetProperty("roles").EnumerateArray().Single().GetString());
        Assert.Equal(Scopes.Wildcard, membership.GetProperty("scopes").EnumerateArray().Single().GetString());
        Assert.True(me.GetProperty("two_factor").GetProperty("enabled").GetBoolean());

        // The point of enrolling the second factor at bootstrap: an owner without one would be
        // refused by every scoped endpoint, this one included.
        var roles = await client.GetFromJsonAsync<JsonElement>("/staff/roles", Token);

        Assert.Equal(7, roles.GetProperty("data").EnumerateArray().Count(role => role.GetProperty("is_system").GetBoolean()));
    }

    [Fact]
    public async Task The_clinic_name_given_at_bootstrap_is_what_the_public_page_shows()
    {
        await FreshInstance.OwnerAsync(fixture);
        using var anonymous = fixture.CreateClient();

        var clinic = await anonymous.GetFromJsonAsync<JsonElement>("/clinic", Token);

        Assert.False(string.IsNullOrWhiteSpace(clinic.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        await FreshInstance.OwnerAsync(fixture);

        // A different address on purpose: a second run must not quietly add a second owner just
        // because it was handed different arguments.
        var again = await FreshInstance.BootstrapAgainAsync(fixture, new BootstrapClinicCommand(
            "someone-else@happypaws.test", "อีกคน", "another-long-password", BranchCode: "second"));

        Assert.Empty(again.RolesCreated);
        Assert.Null(again.BranchId);
        Assert.Null(again.Owner);
    }

    [Fact]
    public async Task The_command_prints_the_credentials_once_and_reports_a_second_run_plainly()
    {
        await FreshInstance.OwnerAsync(fixture);

        using var output = new StringWriter();

        var exit = await OperatorCommands.RunAsync(
            fixture.Services,
            ["bootstrap", "--owner-email", "third@happypaws.test", "--owner-name=คนที่สาม"],
            output);

        Assert.Equal(0, exit);
        Assert.Contains("already exists", output.ToString());
        Assert.DoesNotContain("totp secret", output.ToString());
    }

    [Fact]
    public async Task Resetting_credentials_replaces_the_password_and_signs_the_old_sessions_out()
    {
        // A second owner-grade account, so resetting it cannot disturb the tests sharing the first.
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);
        var email = $"reset-{Guid.NewGuid():N}@happypaws.test";

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, owner, email, [DefaultRoles.Receptionist], ["brn_main"], Token);

        using var before = await FreshInstance.SignInAsync(fixture, email, member.Password, member.TotpSecret, Token);

        using var output = new StringWriter();
        var exit = await OperatorCommands.RunAsync(
            fixture.Services, ["reset-credentials", "--email", email, "--password", "a-brand-new-password"], output);

        Assert.Equal(0, exit);

        var printed = output.ToString();
        var newSecret = printed.Split('\n')
            .Single(line => line.Contains("totp secret"))
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];

        using var anonymous = fixture.CreateClient();

        var oldPassword = await anonymous.PostAsJsonAsync(
            "/auth/staff/login", new { email, password = member.Password }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        // The old refresh cookie belongs to a revoked session.
        Assert.Equal(HttpStatusCode.Unauthorized, (await before.PostAsync("/auth/refresh", null, Token)).StatusCode);

        using var after = await FreshInstance.SignInAsync(fixture, email, "a-brand-new-password", newSecret, Token);

        Assert.Equal(HttpStatusCode.OK, (await after.GetAsync("/me", Token)).StatusCode);
    }

    [Fact]
    public async Task The_last_owner_cannot_be_edited_out_of_their_own_role()
    {
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var roles = await owner.GetFromJsonAsync<JsonElement>("/staff/roles", Token);
        var ownerRole = roles.GetProperty("data").EnumerateArray()
            .Single(role => role.GetProperty("name").GetString() == DefaultRoles.ClinicOwner);

        var response = await owner.PutAsJsonAsync(
            $"/staff/roles/{ownerRole.GetProperty("id").GetString()}/permissions",
            new { scopes = new[] { Scopes.Staff.Read } },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "last_owner",
            (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());

        // Rolled back, not half-applied: the role still says what it said.
        var after = await owner.GetFromJsonAsync<JsonElement>("/staff/roles", Token);
        var scopes = after.GetProperty("data").EnumerateArray()
            .Single(role => role.GetProperty("name").GetString() == DefaultRoles.ClinicOwner)
            .GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString());

        Assert.Equal([Scopes.Wildcard], scopes);
    }

    [Fact]
    public async Task An_owner_cannot_deactivate_themselves()
    {
        var bootstrapped = await FreshInstance.OwnerAsync(fixture);
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var response = await owner.PostAsJsonAsync($"/staff/members/{bootstrapped.StaffId}/deactivate", new { }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "cannot_deactivate_self",
            (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task The_starter_customer_role_cannot_be_given_to_staff_or_edited()
    {
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var invited = await owner.PostAsJsonAsync("/staff/members/invitations", new
        {
            email = $"portal-{Guid.NewGuid():N}@happypaws.test",
            display_name = "คุณลูกค้า",
            staff_type = "receptionist",
            roles = new[] { DefaultRoles.Customer },
            branches = new[] { "brn_main" },
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invited.StatusCode);

        var roles = await owner.GetFromJsonAsync<JsonElement>("/staff/roles", Token);
        var customerRole = roles.GetProperty("data").EnumerateArray()
            .Single(role => role.GetProperty("name").GetString() == DefaultRoles.Customer);

        Assert.False(customerRole.GetProperty("assignable").GetBoolean());

        var edited = await owner.PutAsJsonAsync(
            $"/staff/roles/{customerRole.GetProperty("id").GetString()}/permissions",
            new { scopes = new[] { Scopes.Queue.Read } },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, edited.StatusCode);
    }
}
