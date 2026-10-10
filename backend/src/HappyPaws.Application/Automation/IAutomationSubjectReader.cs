using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>
/// What a rule's subjects are searched by.
/// </summary>
/// <param name="From">Earliest subject instant (appointment or session start), included.</param>
/// <param name="To">Latest subject instant, included.</param>
/// <param name="FirstDay">Earliest due date, for vaccines.</param>
/// <param name="LastDay">Latest due date, for vaccines.</param>
/// <param name="BranchId">Only subjects at this branch; every branch when null.</param>
/// <param name="Species">Only pets of this species.</param>
/// <param name="VaccineName">Only doses of this vaccine, without case.</param>
/// <param name="ServiceId">Only appointments or plans of this service.</param>
/// <param name="UnconfirmedOnly">Only appointments still <c>booked</c>.</param>
public sealed record SubjectQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    DateOnly FirstDay,
    DateOnly LastDay,
    string? BranchId,
    string? Species,
    string? VaccineName,
    string? ServiceId,
    bool UnconfirmedOnly);

/// <summary>
/// One thing a reminder could be about, with what the message needs to name it.
/// </summary>
/// <param name="Kind">Vaccination, appointment or plan session.</param>
/// <param name="SubjectId">Its id; a session is <c>{plan id}:{number}</c>.</param>
/// <param name="StartsAt">When it starts; null for a vaccine's due date.</param>
/// <param name="Day">Its clinic day: the due date, or the day it starts.</param>
/// <param name="CustomerId">Who is told: the pet's primary owner, or the customer the appointment is for.</param>
/// <param name="CustomerName">Their name, for the preview.</param>
/// <param name="PetId">The patient.</param>
/// <param name="PetName">Its name, for the message.</param>
/// <param name="BranchId">
/// The branch it belongs to: the appointment's; for a dose, the branch of the visit it was given
/// in, or else of the pet's latest appointment; for an unbooked session, the plan's visit. Null
/// when nothing ties it to one.
/// </param>
/// <param name="AppointmentId">The appointment, when there is one, so LINE can offer its buttons.</param>
/// <param name="Label">The vaccine's name, or the plan's title.</param>
/// <param name="StaffName">The vet, for an appointment that has one.</param>
/// <param name="SessionNumber">The session's number in its plan.</param>
/// <param name="QuietUntil">
/// When the patient's recorded death was last undone, for a dose or a plan session: a reminder
/// whose send time is at or before it fell due while the record was closed, and is not sent late.
/// Null for appointments, which a death cancels and which are only booked again afterwards. See
/// <c>docs/contract-extensions.md § 29</c>.
/// </param>
public sealed record AutomationSubject(
    AutomationSubjectKind Kind,
    string SubjectId,
    DateTimeOffset? StartsAt,
    DateOnly Day,
    string CustomerId,
    string CustomerName,
    string PetId,
    string PetName,
    string? BranchId,
    string? AppointmentId = null,
    string? Label = null,
    string? StaffName = null,
    int? SessionNumber = null,
    DateTimeOffset? QuietUntil = null);

/// <summary>
/// Finds what a rule's trigger watches, already narrowed by its condition.
/// <para>
/// Excludes what must never be reminded about: deceased patients, merged customer records, doses
/// a later dose of the same vaccine has replaced, and appointments that are no longer going ahead.
/// Read-only; it never loads an aggregate.
/// </para>
/// </summary>
public interface IAutomationSubjectReader
{
    Task<IReadOnlyList<AutomationSubject>> FindAsync(
        AutomationTrigger trigger,
        SubjectQuery query,
        CancellationToken cancellationToken = default);
}
