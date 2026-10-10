using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Forecasting.Attendance;

/// <summary><c>GET /staff/appointments/attendance-risk?branch=&amp;date=</c>.</summary>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
/// <param name="Date">A clinic day, today or later; today when absent.</param>
public sealed record GetAttendanceRiskQuery(string? BranchId = null, DateOnly? Date = null);

/// <summary>
/// <c>GET /staff/appointments/attendance-risk</c>: the day's booked and confirmed visits, each with
/// a risk of not being attended and the reasons, most at risk first.
/// Not in the API reference: <c>docs/contract-extensions.md § 33</c>.
/// </summary>
/// <param name="Date">The clinic day.</param>
/// <param name="BranchId">The branch asked for, when one was.</param>
/// <param name="WindowDays">The cancellation policy's window the strikes are counted over.</param>
/// <param name="Data">One row per appointment: high, then medium, then low; earliest first within each.</param>
public sealed record AttendanceRiskDto(DateOnly Date, string? BranchId, int WindowDays, IReadOnlyList<AttendanceRiskRowDto> Data);

/// <summary>One appointment's risk.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>risk</c>: <c>low</c>, <c>medium</c> or <c>high</c>.</item>
/// <item><c>score</c>: the points behind it (<c>AttendanceRiskRules</c>).</item>
/// <item><c>reasons</c>: one Thai sentence per signal that scored, in the order they were counted.</item>
/// <item><c>signals</c>: the numbers themselves.</item>
/// </list>
/// </remarks>
public sealed record AttendanceRiskRowDto(
    string AppointmentId,
    string BranchId,
    DateTimeOffset StartsAt,
    string Status,
    string CustomerId,
    string CustomerName,
    string PetId,
    string PetName,
    string? StaffId,
    string Risk,
    int Score,
    IReadOnlyList<string> Reasons,
    AttendanceSignalsDto Signals);

/// <param name="NoShows">No-shows inside the window.</param>
/// <param name="LateCancellations">Late cancellations inside the window.</param>
/// <param name="DaysBookedAhead">Clinic days between booking and the visit.</param>
/// <param name="Confirmed">The owner confirmed.</param>
public sealed record AttendanceSignalsDto(int NoShows, int LateCancellations, int DaysBookedAhead, bool Confirmed);

/// <summary>
/// "นัดไหนเสี่ยงไม่มา": the risk of each visit of a day, from the customer's own no-shows and late
/// cancellations, how far ahead it was booked, and whether it was confirmed
/// (<see cref="AttendanceRiskRules"/>). Read-only; nothing is stored and nobody is told.
/// <para>
/// Branch rule of every appointment read: a named branch the caller does not work at is
/// <c>403</c>; with none named, a branch-bound caller sees their own branches. The strikes are the
/// customer's across the clinic, because missing a visit at one branch says the same about them as
/// missing it at another.
/// </para>
/// </summary>
public sealed class GetAttendanceRiskHandler(
    IForecastQueries queries,
    IAppointmentRepository appointments,
    IClinicSettingsStore settings,
    ForecastBranches branches,
    ClinicDays clinicDays,
    IClock clock)
{
    public async Task<AttendanceRiskDto> HandleAsync(GetAttendanceRiskQuery query, CancellationToken cancellationToken = default)
    {
        var today = clinicDays.Today;
        var date = query.Date ?? today;

        if (date < today)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["date"] = ["ดูความเสี่ยงได้ตั้งแต่วันนี้เป็นต้นไป นัดที่ผ่านไปแล้วรู้ผลแล้ว"],
            });
        }

        var scope = branches.Scope(query.BranchId);
        var policy = await settings.CancellationPolicyAsync(cancellationToken);

        var candidates = await queries.AppointmentsForRiskAsync(
            clinicDays.StartOf(date),
            clinicDays.StartOf(date.AddDays(1)),
            scope,
            cancellationToken);

        var counts = candidates.Count == 0
            ? new Dictionary<string, AttendanceCounts>()
            : await appointments.AttendanceForAsync(
                [.. candidates.Select(row => row.CustomerId)],
                clock.UtcNow.AddDays(-policy.WindowDays),
                cancellationToken);

        var rows = candidates.Select(row =>
        {
            var strikes = counts.GetValueOrDefault(row.CustomerId) ?? new AttendanceCounts(0, 0, 0, 0);
            var signals = new AttendanceSignals(
                strikes.NoShowsSince,
                strikes.LateCancellationsSince,
                policy.WindowDays,
                Math.Max(0, clinicDays.DateOf(row.StartsAt).DayNumber - clinicDays.DateOf(row.BookedAt).DayNumber),
                row.Status == nameof(AppointmentStatus.Confirmed));
            var verdict = AttendanceRiskRules.Judge(signals);

            return (verdict.Risk, Row: new AttendanceRiskRowDto(
                row.AppointmentId,
                row.BranchId,
                row.StartsAt,
                AppointmentDto.ToWireStatus(Enum.Parse<AppointmentStatus>(row.Status)),
                row.CustomerId,
                row.CustomerName,
                row.PetId,
                row.PetName,
                row.StaffId,
                WireEnum.Of(verdict.Risk),
                verdict.Score,
                verdict.Reasons,
                new AttendanceSignalsDto(signals.NoShows, signals.LateCancellations, signals.DaysBookedAhead, signals.Confirmed)));
        });

        return new AttendanceRiskDto(
            date,
            query.BranchId is { Length: > 0 } branchId ? branchId.Trim() : null,
            policy.WindowDays,
            [.. rows
                .OrderByDescending(row => row.Risk)
                .ThenBy(row => row.Row.StartsAt)
                .ThenBy(row => row.Row.AppointmentId, StringComparer.Ordinal)
                .Select(row => row.Row)]);
    }
}
