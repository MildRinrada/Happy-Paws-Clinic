using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Appointments.MarkNoShow;

/// <summary><c>POST /staff/appointments/{id}/no-show</c> — "บันทึกไม่มาตามนัด · นับสถิติรายลูกค้า".</summary>
public sealed record MarkNoShowCommand(string AppointmentId);

/// <summary>
/// Records that the customer did not come, and answers with where that leaves them.
/// <para>
/// <b>"นับสถิติรายลูกค้า"</b> is counted from the appointments, not kept in a counter on the
/// customer: a counter is a second copy of what the calendar already says, free to disagree with
/// it the first time a no-show is recorded by mistake. The answer carries the counts and, under the
/// cancellation policy, the fee this strike carries. The fee is reported, not charged.
/// </para>
/// <para>
/// Only after the appointment's start time, and only from <c>booked</c> or <c>confirmed</c>: a pet
/// that checked in did come. Both refusals are <c>409</c>.
/// </para>
/// </summary>
public sealed class MarkNoShowHandler(
    IAppointmentRepository appointments,
    BranchGuard branchGuard,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<NoShowResultDto> HandleAsync(MarkNoShowCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        Appointment? missed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, token);
            branchGuard.EnsureCanAccess(appointment.BranchId);

            appointment.MarkNoShow(now);

            audit.Record(new AuditEvent(
                AuditActions.AppointmentNoShow,
                AuditResources.Appointment,
                appointment.Id,
                appointment.PetId,
                appointment.BranchId,
                new Dictionary<string, object?> { ["customer_id"] = appointment.CustomerId }));

            missed = appointment;
        }, cancellationToken);

        var policy = await settings.CancellationPolicyAsync(cancellationToken);
        var attendance = await AttendanceAsync(appointments, policy, missed!.CustomerId, now, cancellationToken);

        return new NoShowResultDto(AppointmentDto.From(missed), attendance);
    }

    /// <summary>A customer's record under the policy, as of now.</summary>
    public static async Task<CustomerAttendanceDto> AttendanceAsync(
        IAppointmentRepository appointments,
        CancellationPolicy policy,
        string customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var counts = await appointments.AttendanceAsync(customerId, now.AddDays(-policy.WindowDays), cancellationToken);

        return new CustomerAttendanceDto(
            customerId,
            counts.NoShows,
            counts.LateCancellations,
            counts.StrikesSince,
            policy.WindowDays,
            policy.FreeAllowance,
            policy.FeeFor(counts.StrikesSince));
    }
}
