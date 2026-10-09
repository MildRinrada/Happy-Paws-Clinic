namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// The claim names carried in an access token.
/// <para>
/// Shared between the issuer (Infrastructure) and the reader (Api) so the two cannot drift. A
/// typo here would not fail a build — it would silently produce a caller with no scopes, or worse,
/// a caller whose scopes are never read and whose checks therefore never fire.
/// </para>
/// </summary>
public static class ClinicClaimTypes
{
    /// <summary>Account id, e.g. <c>usr_8fA2</c>. Carried in the standard <c>sub</c> claim.</summary>
    public const string Subject = "sub";

    /// <summary>Staff membership id, e.g. <c>stf_204</c>. Absent for a customer-only token.</summary>
    public const string StaffId = "stf";

    /// <summary>Customer membership id, e.g. <c>cus_1180</c>. Absent for a staff-only token.</summary>
    public const string CustomerId = "cus";

    /// <summary>Assigned branch id. Repeated once per branch.</summary>
    public const string Branch = "brn";

    /// <summary>One granted scope. Repeated once per scope, including wildcards.</summary>
    public const string Scope = "scope";

    /// <summary>
    /// Present and <c>"1"</c> only when this token was minted after a successful 2FA step.
    /// A staff token issued by <c>/auth/staff/login</c> alone must never carry it.
    /// </summary>
    public const string TwoFactorSatisfied = "amr_2fa";

    /// <summary>Session id, so a token can be tied back to a revocable device session.</summary>
    public const string SessionId = "sid";

    /// <summary>
    /// The API key a request authenticated with, e.g. <c>key_01J…</c>. Present only on an API key's
    /// principal, never on a person's access token.
    /// </summary>
    public const string ApiKey = "key";
}
