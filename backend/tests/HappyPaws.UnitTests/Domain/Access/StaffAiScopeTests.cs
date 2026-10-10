using HappyPaws.Domain.Access;

namespace HappyPaws.UnitTests.Domain.Access;

/// <summary><c>ai:use</c> (docs/contract-extensions.md § 34): who starts with it, and who can never hold it.</summary>
public class StaffAiScopeTests
{
    private static ScopeSet StarterOf(string role) =>
        new(DefaultRoles.StarterScopes.Single(starter => starter.Key == role).Value);

    [Fact]
    public void It_is_in_the_catalogue_as_its_own_resource()
    {
        Assert.Contains(Scopes.Ai.Use, ScopeCatalogue.All);
        Assert.Equal("ai", ScopeCatalogue.ResourceOf(Scopes.Ai.Use));
        Assert.True(ScopeCatalogue.IsGrantable("ai:*"));
    }

    [Fact]
    public void Vets_receptionists_and_the_owner_start_with_it_and_the_others_do_not()
    {
        Assert.True(StarterOf(DefaultRoles.Veterinarian).Allows(Scopes.Ai.Use));
        Assert.True(StarterOf(DefaultRoles.Receptionist).Allows(Scopes.Ai.Use));
        Assert.True(StarterOf(DefaultRoles.ClinicOwner).Allows(Scopes.Ai.Use));

        Assert.False(StarterOf(DefaultRoles.ClinicAdmin).Allows(Scopes.Ai.Use));
        Assert.False(StarterOf(DefaultRoles.VetAssistant).Allows(Scopes.Ai.Use));
        Assert.False(StarterOf(DefaultRoles.Cashier).Allows(Scopes.Ai.Use));
    }

    [Fact]
    public void An_api_key_can_never_carry_it()
    {
        Assert.False(ApiKeyScopeRules.MayCarry(Scopes.Ai.Use));
        Assert.DoesNotContain(Scopes.Ai.Use, ApiKeyScopeRules.Grantable);
    }
}
