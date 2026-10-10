namespace HappyPaws.Application.Forecasting.Attendance;

/// <summary>How likely an appointment is to go unattended.</summary>
public enum AttendanceRisk
{
    Low,
    Medium,
    High,
}

/// <summary>What is known about one appointment and its customer.</summary>
/// <param name="NoShows">The customer's no-shows inside the cancellation policy's window.</param>
/// <param name="LateCancellations">Their late cancellations inside the same window.</param>
/// <param name="WindowDays">The window, from the cancellation policy.</param>
/// <param name="DaysBookedAhead">Clinic days between booking and the visit.</param>
/// <param name="Confirmed">True when the owner has confirmed the visit.</param>
public sealed record AttendanceSignals(int NoShows, int LateCancellations, int WindowDays, int DaysBookedAhead, bool Confirmed);

/// <summary>The verdict, and the plain-language reasons behind it.</summary>
public sealed record AttendanceVerdict(AttendanceRisk Risk, int Score, IReadOnlyList<string> Reasons);

/// <summary>
/// Rule-based attendance risk for <c>GET /staff/appointments/attendance-risk</c>
/// (<c>docs/contract-extensions.md § 33</c>). Points, so every verdict can be explained line by line:
/// <list type="table">
/// <item><term>each no-show in the window</term><description>2 points</description></item>
/// <item><term>each late cancellation in the window</term><description>1 point</description></item>
/// <item><term>booked <see cref="LongLeadDays"/> days or more ahead</term><description>1 point</description></item>
/// <item><term>not confirmed by the owner</term><description>1 point</description></item>
/// </list>
/// <b>High</b> at <see cref="HighScore"/> points or with <see cref="HighNoShows"/> no-shows on their
/// own; <b>medium</b> at <see cref="MediumScore"/>; otherwise <b>low</b>. Being unconfirmed alone is
/// never more than low, because many owners never press the button. The counts are the advanced
/// booking module's own (<c>IAppointmentRepository.AttendanceAsync</c>): from the appointments
/// table, measured by each visit's start, over the cancellation policy's window.
/// </summary>
public static class AttendanceRiskRules
{
    public const int NoShowPoints = 2;
    public const int LateCancellationPoints = 1;
    public const int LongLeadPoints = 1;
    public const int UnconfirmedPoints = 1;

    public const int LongLeadDays = 30;
    public const int MediumScore = 2;
    public const int HighScore = 4;
    public const int HighNoShows = 2;

    public static AttendanceVerdict Judge(AttendanceSignals signals)
    {
        var score = 0;
        var reasons = new List<string>();

        if (signals.NoShows > 0)
        {
            score += signals.NoShows * NoShowPoints;
            reasons.Add($"ไม่มาตามนัด {signals.NoShows} ครั้งใน {Window(signals.WindowDays)}");
        }

        if (signals.LateCancellations > 0)
        {
            score += signals.LateCancellations * LateCancellationPoints;
            reasons.Add($"ยกเลิกกระชั้น {signals.LateCancellations} ครั้งใน {Window(signals.WindowDays)}");
        }

        if (signals.DaysBookedAhead >= LongLeadDays)
        {
            score += LongLeadPoints;
            reasons.Add($"จองไว้ล่วงหน้า {signals.DaysBookedAhead} วัน");
        }

        if (!signals.Confirmed)
        {
            score += UnconfirmedPoints;
            reasons.Add("เจ้าของยังไม่ได้ยืนยันนัด");
        }

        var risk = score >= HighScore || signals.NoShows >= HighNoShows
            ? AttendanceRisk.High
            : score >= MediumScore
                ? AttendanceRisk.Medium
                : AttendanceRisk.Low;

        return new AttendanceVerdict(risk, score, reasons);
    }

    /// <summary>"12 เดือนที่ผ่านมา" for a year's window, "N วันที่ผ่านมา" for any other.</summary>
    public static string Window(int days) =>
        days is 365 or 366 ? "12 เดือนที่ผ่านมา" : $"{days} วันที่ผ่านมา";
}
