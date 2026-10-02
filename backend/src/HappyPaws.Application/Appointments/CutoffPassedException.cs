using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// A customer tried to move an appointment after the clinic's change cut-off —
/// "ลูกค้าเลื่อนได้ถ้ายังไม่ถึง cutoff".
/// <para>
/// Deliberately not <c>403</c>. Nothing is wrong with the caller's permissions: they own the pet
/// and they own the booking, and they could have done this an hour earlier. Answering "forbidden"
/// would send the frontend down a permissions path when the correct response is "call the clinic".
/// The cut-off and the instant it passed ride in <c>details</c> so that message can be specific.
/// </para>
/// <para>
/// Staff are not subject to this — the counter reschedules for people who telephone, which is the
/// whole reason the cut-off exists.
/// </para>
/// <para>
/// <c>cutoff_passed</c> is not named in the API reference; it is recorded for ratification in
/// <c>docs/assumptions.md § 14</c>.
/// </para>
/// </summary>
public sealed class CutoffPassedException : AppException
{
    public CutoffPassedException(int cutoffHours, DateTimeOffset cutoffAt)
        : base("cutoff_passed", $"เลยกำหนดเปลี่ยนแปลงนัดแล้ว ({cutoffHours} ชั่วโมงก่อนถึงนัด) กรุณาติดต่อคลินิก", HttpStatus.Conflict)
    {
        CutoffHours = cutoffHours;
        CutoffAt = cutoffAt;

        MessageEn = $"Changes close {cutoffHours} hours before the appointment. Please contact the clinic.";
        Details = new Dictionary<string, object>
        {
            ["cutoff_hours"] = cutoffHours,
            ["cutoff_at"] = cutoffAt,
        };
    }

    public int CutoffHours { get; }

    /// <summary>The instant after which the customer could no longer make this change.</summary>
    public DateTimeOffset CutoffAt { get; }
}
