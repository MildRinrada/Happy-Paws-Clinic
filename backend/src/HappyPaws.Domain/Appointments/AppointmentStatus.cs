namespace HappyPaws.Domain.Appointments;

/// <summary>
/// Appointment state machine from the contract:
/// <c>draft → booked → confirmed → checked_in → in_consultation → completed</c>,
/// with <c>cancelled</c> and <c>no_show</c> as terminal exits.
/// Skipping a step answers <c>409 state_conflict</c>.
/// </summary>
public enum AppointmentStatus
{
    Draft,
    Booked,
    Confirmed,
    CheckedIn,
    InConsultation,
    Completed,
    Cancelled,
    NoShow,
}
