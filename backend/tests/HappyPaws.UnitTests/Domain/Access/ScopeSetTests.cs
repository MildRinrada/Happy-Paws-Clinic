using HappyPaws.Domain.Access;

namespace HappyPaws.UnitTests.Domain.Access;

/// <summary>
/// <see cref="ScopeSet"/> is the scope-level security boundary for all 164 endpoints. A wildcard
/// that matches too widely is a privilege escalation; one that matches too narrowly locks staff
/// out mid-consultation. Both directions are asserted here.
/// </summary>
public class ScopeSetTests
{
    [Fact]
    public void Exact_scope_is_allowed()
    {
        var scopes = new ScopeSet([Scopes.Appointment.Write]);

        Assert.True(scopes.Allows(Scopes.Appointment.Write));
    }

    [Fact]
    public void Unrelated_scope_is_refused()
    {
        var scopes = new ScopeSet([Scopes.Appointment.Write]);

        Assert.False(scopes.Allows(Scopes.Record.Sign));
    }

    [Fact]
    public void Owner_wildcard_allows_everything()
    {
        var owner = new ScopeSet([Scopes.Wildcard]);

        Assert.True(owner.IsUnrestricted);
        Assert.True(owner.Allows(Scopes.Record.Sign));
        Assert.True(owner.Allows(Scopes.Payment.Payout));
    }

    [Fact]
    public void Resource_wildcard_allows_every_action_on_that_resource()
    {
        // The contract grants receptionists `queue:*`.
        var receptionist = new ScopeSet(["queue:*"]);

        Assert.True(receptionist.Allows(Scopes.Queue.Read));
        Assert.True(receptionist.Allows(Scopes.Queue.Write));
        Assert.True(receptionist.Allows(Scopes.Queue.Call));
    }

    [Fact]
    public void Resource_wildcard_does_not_leak_to_another_resource()
    {
        // The bug this guards: prefix matching that treats `record:*` as covering `record_audit`
        // or `records:read`. `consult:*` must not imply anything outside `consult:`.
        var vet = new ScopeSet(["consult:*"]);

        Assert.False(vet.Allows(Scopes.Record.Sign));
        Assert.False(vet.Allows(Scopes.Payment.Refund));
        Assert.False(vet.Allows("consultation:read"));
    }

    [Fact]
    public void Wildcard_on_one_resource_is_not_the_global_wildcard()
    {
        var cashier = new ScopeSet(["invoice:*", "payment:*"]);

        Assert.False(cashier.IsUnrestricted);
        Assert.False(cashier.Allows(Scopes.Record.Read));
    }

    [Fact]
    public void Empty_set_allows_nothing()
    {
        Assert.False(ScopeSet.Empty.Allows(Scopes.Queue.Read));
        Assert.False(ScopeSet.Empty.IsUnrestricted);
    }

    [Fact]
    public void Missing_scopes_are_reported_for_the_403_body()
    {
        var assistant = new ScopeSet([Scopes.Queue.Read, Scopes.Patient.Read]);

        var missing = assistant.MissingFrom([Scopes.Queue.Read, Scopes.Record.Write, Scopes.Prescription.Write]);

        Assert.Equal([Scopes.Record.Write, Scopes.Prescription.Write], missing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("noresourcepart")]
    public void Malformed_required_scope_is_refused_not_accidentally_matched(string required)
    {
        var scopes = new ScopeSet([Scopes.Queue.Read]);

        Assert.False(scopes.Allows(required));
    }

    [Fact]
    public void Scope_comparison_is_case_sensitive()
    {
        // Scope strings are an exact contract. Case-insensitive matching would let `RECORD:SIGN`
        // through a system that only ever audits the lowercase form.
        var scopes = new ScopeSet([Scopes.Record.Sign]);

        Assert.False(scopes.Allows("RECORD:SIGN"));
    }
}
