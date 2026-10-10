using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GuestBooking;
using HappyPaws.Application.Identity.Dtos;

namespace HappyPaws.Api.Contracts.Appointments;

/// <summary>
/// Request bodies for the Booking and Appointment endpoints.
/// <para>
/// The API reference specifies these endpoints, their scopes and their behaviour, but does not
/// print their request schemas. The field names here are taken from the vocabulary the document
/// does use — <c>starts_at</c>, <c>service</c>, <c>vet_id</c>, <c>branch</c> — and are recorded in
/// <c>docs/assumptions.md</c> as an inferred contract for the owner to confirm.
/// </para>
/// <para>
/// Property names reach the wire in snake_case through the serializer policy set in
/// <c>Program.cs</c>: <c>ServiceId</c> binds from <c>service_id</c>.
/// </para>
/// <para>
/// <c>HoldId</c> is the ten-minute hold being redeemed, present when the booking came from the web
/// form and absent when it did not. <c>Type</c> is an appointment type code from
/// <c>PUT /staff/appointment-types</c>; omitted, it is <c>regular</c>.
/// </para>
/// <para>
/// <c>NewPet</c> replaces <c>PetId</c> for a pet the clinic has not met yet; it is not in the API
/// reference (<c>docs/contract-extensions.md § 27</c>).
/// </para>
/// </summary>
public sealed record BookAppointmentRequest(
    string? PetId,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null,
    string? HoldId = null,
    string? Reason = null,
    string? Type = null,
    NewPetRequest? NewPet = null);

/// <summary><c>new_pet</c> on <c>POST /portal/appointments</c>: a name and a species, e.g. <c>สุนัข</c>.</summary>
public sealed record NewPetRequest(string Name, string Species);

/// <summary>Request body for <c>POST /booking/holds</c>.</summary>
public sealed record HoldSlotRequest(
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null);

/// <summary>Request body for <c>POST /staff/appointments</c>. <c>Type</c> defaults to <c>regular</c>.</summary>
public sealed record CreateStaffAppointmentRequest(
    string PetId,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null,
    string? Reason = null,
    bool WalkIn = false,
    string? Type = null);

/// <summary>
/// <c>POST /staff/appointments/recurring</c> — "คอร์สรักษาต่อเนื่อง เช่น เคมีบำบัดทุก 3 สัปดาห์".
/// <c>rrule</c> repeats the first visit, e.g. <c>FREQ=WEEKLY;INTERVAL=3;COUNT=6</c>.
/// </summary>
public sealed record CreateRecurringSeriesRequest(
    string PetId,
    string ServiceId,
    DateTimeOffset FirstStartsAt,
    string Rrule,
    string? StaffId = null,
    string? BranchId = null,
    string? Reason = null,
    string? Type = null);

/// <summary>
/// <c>POST /staff/waiting-list</c> — "คิวรอคิวว่าง". The window defaults to today and the next
/// fourteen days; <c>branch_id</c> and <c>staff_id</c> narrow which openings count.
/// </summary>
public sealed record AddToWaitingListRequest(
    string PetId,
    string ServiceId,
    DateOnly? EarliestOn = null,
    DateOnly? LatestOn = null,
    string? BranchId = null,
    string? StaffId = null,
    string? Note = null);

/// <summary><c>PUT /staff/appointment-types</c> — the whole set. <c>regular</c> must be in it.</summary>
public sealed record ReplaceAppointmentTypesRequest(IReadOnlyList<AppointmentTypeRequest> Types);

/// <summary>
/// One appointment type. <c>queue_priority</c> is <c>normal</c>, <c>urgent</c> or <c>emergency</c>;
/// <c>duration_minutes</c> and <c>lead_time_minutes</c> null mean the service's and the clinic's own.
/// </summary>
public sealed record AppointmentTypeRequest(
    string Code,
    string Name,
    int? DurationMinutes = null,
    bool OnlineBooking = true,
    int? LeadTimeMinutes = null,
    string? QueuePriority = null);

/// <summary>
/// <c>PUT /staff/policies/cancellation</c> — "cutoff ค่าปรับ และจำนวนครั้งที่ยอมให้". Every field is
/// required: this replaces the policy.
/// </summary>
public sealed record ReplaceCancellationPolicyRequest(
    int CutoffHours,
    long FeeSatang,
    int FreeAllowance,
    int WindowDays);

/// <summary>Request body for <c>POST /appointments/{id}/reschedule</c>.</summary>
public sealed record RescheduleAppointmentRequest(DateTimeOffset StartsAt, string? StaffId = null);

/// <summary>Request body for <c>POST /appointments/{id}/cancel</c>.</summary>
public sealed record CancelAppointmentRequest(string? Reason = null);

/// <summary>Request body for <c>PATCH /staff/appointments/{id}</c>.</summary>
public sealed record UpdateAppointmentRequest(
    string? ServiceId = null,
    string? StaffId = null,
    string? Reason = null);

/// <summary>
/// <c>POST /booking/guest/start</c> (<c>docs/contract-extensions.md § 27</c>): the time a visitor
/// picked and the phone the code goes to.
/// </summary>
public sealed record StartGuestBookingRequest(
    string Phone,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null);

/// <summary>
/// <c>POST /booking/guest/verify</c> (<c>docs/contract-extensions.md § 27</c>). <c>display_name</c>
/// and <c>accept_privacy_notice</c> are required of everyone; they are used only when the number has
/// no customer login yet.
/// </summary>
public sealed record VerifyGuestBookingRequest(
    string HoldId,
    string Phone,
    string Code,
    string DisplayName,
    bool AcceptPrivacyNotice = false,
    string? PolicyVersion = null,
    bool MarketingConsent = false);

/// <summary>
/// The answer to <c>POST /booking/guest/verify</c>: the same token fields as sign-in (the refresh
/// token travels in its cookie, as there), plus what the booking form needs next.
/// </summary>
public sealed record GuestBookingVerifiedResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    bool TwoFactorSatisfied,
    SessionCapabilities? Capabilities,
    bool AccountCreated,
    IReadOnlyList<GuestPetDto> Pets,
    SlotHoldDto Hold)
{
    public static GuestBookingVerifiedResponse From(GuestBookingVerifiedResult result) =>
        new(
            result.Tokens.AccessToken,
            "Bearer",
            result.Tokens.AccessTokenExpiresAt,
            result.Tokens.TwoFactorSatisfied,
            result.Tokens.Capabilities,
            result.AccountCreated,
            result.Pets,
            result.Hold);
}
