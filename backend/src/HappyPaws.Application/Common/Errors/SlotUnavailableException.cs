namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The requested appointment time is not bookable. This is the failure the contract uses as its
/// worked example of the error envelope:
/// <code>
/// { "code": "slot_unavailable", "message": "ช่วงเวลานี้ถูกจองแล้ว",
///   "field": "starts_at", "details": { "suggested": ["11:00","15:30","17:00"] } }
/// </code>
/// <para>
/// <c>details.suggested</c> is not decoration. A booking form that answers "taken" and stops makes
/// the customer guess again; one that answers with three real openings turns the failure into the
/// next step. The suggestions come from the same availability calculation that rejected the
/// request, so they are as current as the refusal.
/// </para>
/// <para>
/// Times are formatted <c>HH:mm</c> in clinic-local time, exactly as the contract's example shows.
/// The suggestion list rides in <see cref="AppException.Details"/> so the shared envelope writer
/// emits it with no special case.
/// </para>
/// </summary>
public sealed class SlotUnavailableException(string messageTh, IReadOnlyList<string> suggested)
    : AppException(ErrorCodes.SlotUnavailable, messageTh, HttpStatus.UnprocessableEntity)
{
    /// <summary>Alternative start times on the same day, in <c>HH:mm</c> clinic-local form.</summary>
    public IReadOnlyList<string> Suggested { get; } = suggested;

    public static SlotUnavailableException AlreadyBooked(IReadOnlyList<string> suggested) =>
        new("ช่วงเวลานี้ถูกจองแล้ว", suggested)
        {
            MessageEn = "Slot already booked",
            Field = "starts_at",
            Details = Detail(suggested),
        };

    /// <summary>No vet is rostered over the requested window at all.</summary>
    public static SlotUnavailableException OutsideShift(IReadOnlyList<string> suggested) =>
        new("ไม่มีตารางเวรรองรับช่วงเวลานี้", suggested)
        {
            MessageEn = "No staff is rostered for the requested time",
            Field = "starts_at",
            Details = Detail(suggested),
        };

    /// <summary>Inside the booking lead time, or beyond how far ahead the clinic takes bookings.</summary>
    public static SlotUnavailableException OutsideBookingWindow(
        string messageTh,
        string messageEn,
        IReadOnlyList<string> suggested) =>
        new(messageTh, suggested)
        {
            MessageEn = messageEn,
            Field = "starts_at",
            Details = Detail(suggested),
        };

    /// <summary>
    /// The day is a declared holiday — "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ". No suggestions: every
    /// other time that day is closed too, and offering a different day is the booking page's job.
    /// </summary>
    public static SlotUnavailableException ClinicClosed(string holidayName) =>
        new($"คลินิกปิดทำการในวันดังกล่าว ({holidayName})", [])
        {
            MessageEn = "The clinic is closed on that day",
            Field = "starts_at",
            Details = Detail([]),
        };

    /// <summary>
    /// Some visits in a course of appointments cannot be booked, so none are. <c>details.conflicts</c>
    /// lists each one with its own reason and alternatives; <c>details.suggested</c> is empty because
    /// there is no single day to suggest times on.
    /// </summary>
    public static SlotUnavailableException ForSeries(IReadOnlyList<IReadOnlyDictionary<string, object>> conflicts) =>
        new($"นัด {conflicts.Count} ครั้งในชุดนี้จองไม่ได้ จึงยังไม่ได้จองครั้งใดเลย", [])
        {
            MessageEn = "Some visits in the series cannot be booked, so none were",
            Field = "starts_at",
            Details = new Dictionary<string, object> { ["suggested"] = Array.Empty<string>(), ["conflicts"] = conflicts },
        };

    private static Dictionary<string, object> Detail(IReadOnlyList<string> suggested) =>
        new() { ["suggested"] = suggested };
}
