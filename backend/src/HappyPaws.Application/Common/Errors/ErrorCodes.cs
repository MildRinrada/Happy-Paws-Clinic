namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The <c>error.code</c> values this API can return.
/// <para>
/// Every endpoint shares one error envelope, so codes are part of the contract, not free text.
/// Clients switch on <c>code</c>; <c>message</c> (Thai) and <c>message_en</c> are for humans.
/// </para>
/// <para>
/// Codes are in two groups. The first is named explicitly in
/// <c>API Reference.dc.html</c>. The second is required to run the platform but is <b>not</b> in
/// the reference — those are recorded in <c>docs/assumptions.md § 14</c> for the contract owner to
/// ratify. Do not add a third group casually: every code here is something a client may branch on.
/// </para>
/// </summary>
public static class ErrorCodes
{
    // ---- Named in the API reference ----------------------------------------------------------

    /// <summary>401 — token expired or revoked.</summary>
    public const string Unauthenticated = "unauthenticated";

    /// <summary>403 — caller is missing scopes; the missing ones are listed in <c>details.missing_scopes</c>.</summary>
    public const string InsufficientScope = "insufficient_scope";

    /// <summary>409 — illegal state transition, e.g. closing a case that was never called.</summary>
    public const string StateConflict = "state_conflict";

    /// <summary>423 — the medical record is signed; changes must go through an amendment.</summary>
    public const string RecordLocked = "record_locked";

    /// <summary>422 — the requested slot was taken; <c>details.suggested</c> offers alternatives.</summary>
    public const string SlotUnavailable = "slot_unavailable";

    // ---- Extensions, pending ratification (docs/assumptions.md § 14) -------------------------

    /// <summary>409 — <c>If-Match</c> did not match the stored <c>version</c>.</summary>
    public const string VersionConflict = "version_conflict";

    /// <summary>422 — request body failed validation; <c>field</c> names the first offender.</summary>
    public const string ValidationFailed = "validation_failed";

    /// <summary>404 — resource absent, or hidden because the caller may not know it exists.</summary>
    public const string NotFound = "not_found";

    /// <summary>403 — the caller holds the scope but not on this particular record.</summary>
    public const string Forbidden = "forbidden";

    /// <summary>400 — a money/appointment/queue POST arrived without <c>Idempotency-Key</c>.</summary>
    public const string IdempotencyKeyRequired = "idempotency_key_required";

    /// <summary>409 — the same <c>Idempotency-Key</c> was reused with a different request body.</summary>
    public const string IdempotencyKeyReused = "idempotency_key_reused";

    /// <summary>409 — an identical request is still in flight; retry once it settles.</summary>
    public const string RequestInFlight = "request_in_flight";

    /// <summary>429 — rate limit exceeded.</summary>
    public const string RateLimited = "rate_limited";

    /// <summary>500 — unexpected fault. Never carries internal detail.</summary>
    public const string InternalError = "internal_error";

    /// <summary>409 — a new shift overlaps one the person already has; <c>details.conflicts</c> lists them.</summary>
    public const string ShiftOverlap = "shift_overlap";

    /// <summary>409 — a new shift falls in time off already recorded; <c>details.time_off</c> lists it.</summary>
    public const string StaffOnTimeOff = "staff_on_time_off";

    /// <summary>409 — the email or phone number already belongs to another account.</summary>
    public const string ContactTaken = "contact_taken";

    /// <summary>409 — a broadcast would pass the month's quota; <c>details</c> has the limit and what is used.</summary>
    public const string QuotaExceeded = "quota_exceeded";

    /// <summary>409 — a LINE broadcast was asked for, but LINE has no access token or is switched off.</summary>
    public const string LineNotConfigured = "line_not_configured";

    /// <summary>409 — a new inventory item has the name of one already in the catalogue; <c>details.item_id</c> is that one.</summary>
    public const string InventoryItemExists = "inventory_item_exists";

    /// <summary>409 — an issue or transfer asks for more usable units than the branch holds; <c>details.available</c> says how many.</summary>
    public const string InsufficientStock = "insufficient_stock";

    /// <summary>
    /// 409 — the LINE address is already on another customer's record; <c>details.customer_id</c>
    /// names it when known. See <c>docs/contract-extensions.md § 18</c>.
    /// </summary>
    public const string LineAddressTaken = "line_address_taken";

    /// <summary>409 — a new coupon asks for a code another coupon already has.</summary>
    public const string CouponCodeTaken = "coupon_code_taken";
}
