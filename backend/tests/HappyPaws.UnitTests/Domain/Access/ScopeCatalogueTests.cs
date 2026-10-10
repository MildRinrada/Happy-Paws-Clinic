using HappyPaws.Domain.Access;

namespace HappyPaws.UnitTests.Domain.Access;

/// <summary>
/// The catalogue is what a role may contain, so it is asserted like the security boundary it is.
/// A scope missing from it is an endpoint no role can ever be granted; a value wrongly accepted by
/// it is a permission that means nothing and looks like it means something.
/// </summary>
public class ScopeCatalogueTests
{
    [Fact]
    public void Every_scope_is_a_resource_and_an_action()
    {
        Assert.NotEmpty(ScopeCatalogue.All);

        Assert.All(ScopeCatalogue.All, scope =>
        {
            var parts = scope.Split(':');

            Assert.Equal(2, parts.Length);
            Assert.All(parts, part => Assert.False(string.IsNullOrWhiteSpace(part)));
        });
    }

    [Fact]
    public void The_catalogue_has_no_duplicates()
    {
        Assert.Equal(ScopeCatalogue.All.Count, ScopeCatalogue.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Scopes_that_endpoints_require_are_in_the_catalogue()
    {
        // Read off the constants by reflection, so this is really asserting that the reflection
        // reaches every nested group — a group it missed would be a set of endpoints no role
        // could be granted.
        Assert.Contains(Scopes.Record.Sign, ScopeCatalogue.All);
        Assert.Contains(Scopes.Payment.Payout, ScopeCatalogue.All);
        Assert.Contains(Scopes.Settings.Write, ScopeCatalogue.All);
        Assert.Contains(Scopes.Role.Manage, ScopeCatalogue.All);
        Assert.Contains(Scopes.Audit.Read, ScopeCatalogue.All);
    }

    [Fact]
    public void The_wildcard_and_the_portal_marker_are_not_catalogue_entries()
    {
        // `*` is a way of holding scopes, not a scope. `portal` comes from being a customer.
        Assert.DoesNotContain(Scopes.Wildcard, ScopeCatalogue.All);
        Assert.DoesNotContain(Scopes.Portal, ScopeCatalogue.All);
    }

    [Theory]
    [InlineData("consult:write")]
    [InlineData("*")]
    [InlineData("consult:*")]
    [InlineData("invoice:*")]
    public void A_role_may_carry_a_catalogue_scope_or_either_wildcard_form(string scope)
    {
        Assert.True(ScopeCatalogue.IsGrantable(scope));
    }

    [Theory]
    [InlineData("")]
    [InlineData("portal")]
    [InlineData("consult:fly")]
    [InlineData("nonsense:*")]
    [InlineData("consult:")]
    [InlineData(":*")]
    [InlineData("CONSULT:WRITE")]
    public void A_role_may_not_carry_anything_else(string scope)
    {
        Assert.False(ScopeCatalogue.IsGrantable(scope));
    }

    [Fact]
    public void Unknown_scopes_are_named_for_the_refusal()
    {
        var unknown = ScopeCatalogue.UnknownIn([Scopes.Queue.Read, "queue:fly", "nonsense:*", "queue:fly"]);

        Assert.Equal(["queue:fly", "nonsense:*"], unknown);
    }
}

/// <summary>
/// The seven starter roles, held to the contract's "Role ตั้งต้น" table.
/// </summary>
public class DefaultRolesTests
{
    private static IReadOnlyList<string> ScopesOf(string role) =>
        DefaultRoles.StarterScopes.Single(starter => starter.Key == role).Value;

    [Fact]
    public void All_seven_are_defined_in_the_contracts_order()
    {
        Assert.Equal(
            [
                DefaultRoles.ClinicOwner, DefaultRoles.ClinicAdmin, DefaultRoles.Veterinarian,
                DefaultRoles.Receptionist, DefaultRoles.VetAssistant, DefaultRoles.Cashier,
                DefaultRoles.Customer,
            ],
            DefaultRoles.StarterScopes.Select(starter => starter.Key));
    }

    [Fact]
    public void The_owner_holds_everything_and_nothing_else_does()
    {
        Assert.Equal([Scopes.Wildcard], ScopesOf(DefaultRoles.ClinicOwner));

        Assert.All(
            DefaultRoles.StarterScopes.Where(starter => starter.Key != DefaultRoles.ClinicOwner),
            starter => Assert.DoesNotContain(Scopes.Wildcard, starter.Value));
    }

    [Fact]
    public void The_admin_holds_everything_except_the_two_the_contract_withholds_and_ai_use()
    {
        var admin = new ScopeSet(ScopesOf(DefaultRoles.ClinicAdmin));

        // "ทุกอย่างยกเว้น billing:payout, settings:danger". ai:use (§ 34) was approved for vets,
        // the counter and the owner only.
        Assert.False(admin.Allows(Scopes.Payment.Payout));
        Assert.False(admin.Allows(Scopes.Settings.Danger));
        Assert.False(admin.Allows(Scopes.Ai.Use));
        Assert.False(admin.IsUnrestricted);

        Assert.All(
            ScopeCatalogue.All.Where(scope => scope is not (Scopes.Payment.Payout or Scopes.Settings.Danger or Scopes.Ai.Use)),
            scope => Assert.True(admin.Allows(scope)));
    }

    [Fact]
    public void Every_staff_role_carries_only_scopes_a_role_may_carry()
    {
        // Otherwise the bootstrap would seed a role that PUT /staff/roles/{id}/permissions would
        // refuse to save back unchanged.
        Assert.All(
            DefaultRoles.StarterScopes.Where(starter => starter.Key != DefaultRoles.Customer),
            starter => Assert.Empty(ScopeCatalogue.UnknownIn(starter.Value)));
    }

    [Fact]
    public void The_customer_role_is_the_portal_and_nothing_a_staff_member_could_use()
    {
        Assert.Equal([Scopes.Portal], ScopesOf(DefaultRoles.Customer));
        Assert.True(Role.Create(DefaultRoles.Customer, ScopesOf(DefaultRoles.Customer)).IsPortalRole);
    }

    [Fact]
    public void A_cashier_can_take_money_and_a_receptionist_as_written_cannot()
    {
        // The contract's own inconsistency, pinned so nobody "fixes" it by accident: the payment
        // endpoint requires payment:collect and the receptionist is given invoice:collect. See
        // docs/assumptions.md § 3.
        Assert.True(new ScopeSet(ScopesOf(DefaultRoles.Cashier)).Allows(Scopes.Payment.Collect));
        Assert.False(new ScopeSet(ScopesOf(DefaultRoles.Receptionist)).Allows(Scopes.Payment.Collect));
    }
}
