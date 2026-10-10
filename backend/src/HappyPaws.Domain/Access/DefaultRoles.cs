namespace HappyPaws.Domain.Access;

/// <summary>
/// The seven starter roles named by the API contract ("Role ตั้งต้น").
/// Roles are data, not code: a clinic owner may create further roles through
/// <c>POST /staff/roles</c> and rewrite any role's scope set through
/// <c>PUT /staff/roles/{id}/permissions</c>. These constants exist so that seeding and
/// authorization tests refer to the same strings the contract uses.
/// </summary>
public static class DefaultRoles
{
    /// <summary>Holds <c>*</c> — every scope in the catalogue.</summary>
    public const string ClinicOwner = "clinic_owner";

    /// <summary>Everything except <c>billing:payout</c> and <c>settings:danger</c>.</summary>
    public const string ClinicAdmin = "clinic_admin";

    /// <summary><c>consult:*</c>, <c>record:write</c>, <c>lab:*</c>, <c>rx:write</c>, <c>queue:call</c>.</summary>
    public const string Veterinarian = "veterinarian";

    /// <summary><c>appointment:*</c>, <c>queue:*</c>, <c>customer:*</c>, <c>invoice:collect</c>.</summary>
    public const string Receptionist = "receptionist";

    /// <summary><c>queue:read</c>, <c>patient:read</c>, <c>vitals:write</c>.</summary>
    public const string VetAssistant = "vet_assistant";

    /// <summary><c>invoice:*</c>, <c>payment:*</c>, <c>receipt:issue</c>.</summary>
    public const string Cashier = "cashier";

    /// <summary>Portal scope only, scoped further to the pets this account cares for.</summary>
    public const string Customer = "customer";

    /// <summary>
    /// The scope set each starter role ships with, copied from the contract's "Role ตั้งต้น" table
    /// row for row. Seeded once by the bootstrap command; after that the rows are the clinic's to
    /// edit, and this table is never consulted for an access decision.
    /// <para>
    /// <c>clinic_admin</c> is the one entry defined by subtraction ("ทุกอย่างยกเว้น …"), so it is
    /// expanded here against the catalogue as it stands at seeding time. Unlike <c>invoice:*</c>, it
    /// does not pick up scopes added in a later release on its own.
    /// </para>
    /// <para>
    /// <c>ai:use</c> on the veterinarian and the receptionist is not from the contract's table: it
    /// was approved with the staff AI assistant (<c>docs/contract-extensions.md § 34</c>), and the
    /// migration <c>StaffAiAssistant</c> grants the same to clinics seeded before it. The owner
    /// approved it for vets, the counter and the owner only, so <c>clinic_admin</c> leaves it out
    /// too; an admin who wants it can add it to a role.
    /// </para>
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> StarterScopes { get; } =
    [
        new(ClinicOwner, [Scopes.Wildcard]),
        new(ClinicAdmin,
        [
            .. ScopeCatalogue.All.Where(scope =>
                scope is not (Scopes.Payment.Payout or Scopes.Settings.Danger or Scopes.Ai.Use)),
        ]),
        new(Veterinarian,
            ["consult:*", Scopes.Record.Write, "lab:*", Scopes.Prescription.Write, Scopes.Queue.Call, Scopes.Ai.Use]),
        new(Receptionist, ["appointment:*", "queue:*", "customer:*", Scopes.Invoice.Collect, Scopes.Ai.Use]),
        new(VetAssistant, [Scopes.Queue.Read, Scopes.Patient.Read, Scopes.Vitals.Write]),
        new(Cashier, ["invoice:*", "payment:*", Scopes.Receipt.Issue]),
        new(Customer, [Scopes.Portal]),
    ];
}
