namespace HappyPaws.Domain.Access;

/// <summary>
/// Scopes that read or write clinical and financial records, or change who can do what. A person
/// holding any of them must have passed 2FA in the session they act from
/// (<c>TwoFactorAuthorizationHandler</c>), and an API key may not carry them at all
/// (<see cref="ApiKeyScopeRules"/>), because a key has no second factor to pass.
/// </summary>
public static class SecondFactorScopes
{
    public static IReadOnlyList<string> All { get; } =
    [
        Scopes.Record.Read,
        Scopes.Record.Write,
        Scopes.Record.Sign,
        Scopes.Record.Amend,
        Scopes.Record.Audit,
        Scopes.Consult.Write,
        Scopes.Prescription.Write,
        Scopes.Prescription.Dispense,
        Scopes.Payment.Refund,
        Scopes.Payment.Payout,
        Scopes.Settings.Danger,
        Scopes.User.Manage,
        Scopes.Role.Manage,
    ];
}

/// <summary>
/// Which scopes an API key may carry.
/// <para>
/// Anything in the catalogue that its creator holds, except: the 2FA-level scopes
/// (<see cref="SecondFactorScopes"/>), <c>integration:manage</c> (a key that can mint keys or
/// repoint webhooks would outlive anyone's attempt to shut it out) and <c>staff:manage</c> (a key
/// that can invite staff can make itself a person), and <c>ai:use</c> (the AI allowance is a monthly
/// budget for people at the clinic; a script holding a key could spend it in an afternoon, see
/// <c>docs/contract-extensions.md § 34</c>). Wildcards are refused too: a key lists what it
/// does, so a scope added in a later release is not granted to it silently.
/// </para>
/// </summary>
public static class ApiKeyScopeRules
{
    private static readonly HashSet<string> Withheld = new(
        [.. SecondFactorScopes.All, Scopes.Integration.Manage, Scopes.Staff.Manage, Scopes.Ai.Use],
        StringComparer.Ordinal);

    /// <summary>True when a key may carry <paramref name="scope"/>, whoever creates it.</summary>
    public static bool MayCarry(string scope) =>
        !Withheld.Contains(scope) && ScopeCatalogue.All.Contains(scope, StringComparer.Ordinal);

    /// <summary>The catalogue scopes a key may ever carry, for the settings page.</summary>
    public static IReadOnlyList<string> Grantable { get; } = [.. ScopeCatalogue.All.Where(MayCarry)];
}
