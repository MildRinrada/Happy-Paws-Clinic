using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.Dtos;

/// <summary>
/// An appointment as the API returns it. Property names reach the wire in snake_case
/// (<c>starts_at</c>, <c>staff_id</c>) through the serializer policy configured in
/// <c>Program.cs</c>.
/// <para>
/// <c>Version</c> is here because the contract requires every mutable resource to carry one for
/// <c>If-Match</c>. It is also returned as an <c>ETag</c> header; the body copy is what a client
/// keeps alongside the record it is editing.
/// </para>
/// </summary>
public sealed record AppointmentDto(
    string Id,
    uint Version,
    string Status,
    string BranchId,
    string CustomerId,
    string PetId,
    string ServiceId,
    string? StaffId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Reason,
    bool WalkIn,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    string Type,
    string? SeriesId,
    bool LateCancellation)
{
    public static AppointmentDto From(Appointment appointment) =>
        new(
            appointment.Id,
            appointment.Version,
            ToWireStatus(appointment.Status),
            appointment.BranchId,
            appointment.CustomerId,
            appointment.PetId,
            appointment.ServiceId,
            appointment.StaffId,
            appointment.StartsAt,
            appointment.EndsAt,
            appointment.Reason,
            appointment.IsWalkIn,
            appointment.ConfirmedAt,
            appointment.CheckedInAt,
            appointment.CancelledAt,
            appointment.CancellationReason,
            appointment.TypeCode,
            appointment.SeriesId,
            appointment.IsLateCancellation);

    /// <summary>
    /// <c>CheckedIn</c> → <c>checked_in</c>. The contract writes its state machine in snake_case
    /// and clients filter on those exact strings, so the conversion happens here rather than
    /// relying on however a serializer happens to spell an enum.
    /// </summary>
    public static string ToWireStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Draft => "draft",
        AppointmentStatus.Booked => "booked",
        AppointmentStatus.Confirmed => "confirmed",
        AppointmentStatus.CheckedIn => "checked_in",
        AppointmentStatus.InConsultation => "in_consultation",
        AppointmentStatus.Completed => "completed",
        AppointmentStatus.Cancelled => "cancelled",
        AppointmentStatus.NoShow => "no_show",
        _ => status.ToString().ToLowerInvariant(),
    };

    /// <summary>Parses the <c>?status=</c> filter. Returns null for an unrecognised value.</summary>
    public static AppointmentStatus? ParseWireStatus(string? status) => status switch
    {
        "draft" => AppointmentStatus.Draft,
        "booked" => AppointmentStatus.Booked,
        "confirmed" => AppointmentStatus.Confirmed,
        "checked_in" => AppointmentStatus.CheckedIn,
        "in_consultation" => AppointmentStatus.InConsultation,
        "completed" => AppointmentStatus.Completed,
        "cancelled" => AppointmentStatus.Cancelled,
        "no_show" => AppointmentStatus.NoShow,
        _ => null,
    };
}
