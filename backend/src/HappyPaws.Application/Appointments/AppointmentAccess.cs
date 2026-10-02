using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments;

/// <summary>Which side of the contract's two audiences a caller was admitted as.</summary>
public enum AppointmentAudience
{
    Staff,
    Customer,
}

/// <summary>
/// The resource-level gate for the five endpoints the contract marks "ทั้งสองฝั่ง" —
/// <c>GET /appointments/{id}</c> and the reschedule, cancel, confirm and check-in actions.
/// <para>
/// Those routes serve staff and customers through the same path, so the scope attribute on the
/// action cannot decide access on its own: a receptionist is admitted by scope and constrained by
/// branch, while an owner holds no staff scope at all and is constrained to their own pets. This
/// class is where that fork lives, so each use case asks once instead of re-deriving it.
/// </para>
/// <para>
/// A customer who asks for someone else's appointment gets <c>404</c>, never <c>403</c>. Appointment
/// ids are sequential-ish and guessable in bulk; answering "forbidden" would confirm which ones are
/// real and turn the endpoint into a census of the clinic's bookings.
/// </para>
/// <para>
/// Ownership is checked against the <b>pet</b>, not against the appointment's <c>customer_id</c>.
/// The contract's rule is "ลูกค้าเห็นเฉพาะนัดของสัตว์ตัวเอง", and care is shared: a co-owner added
/// through <c>POST /staff/pets/{id}/owners</c> must be able to see the visit even though the other
/// owner booked it.
/// </para>
/// </summary>
public sealed class AppointmentAccess(
    ICurrentUser currentUser,
    BranchGuard branchGuard,
    IPetAccessPolicy petAccess)
{
    /// <summary>
    /// Admits the caller to an appointment, or throws. <paramref name="staffScope"/> is the scope
    /// that would admit them as staff; without it they are judged as a customer.
    /// </summary>
    public async Task<AppointmentAudience> AuthoriseAsync(
        Appointment appointment,
        string staffScope,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.StaffId is { Length: > 0 } && currentUser.Scopes.Allows(staffScope))
        {
            branchGuard.EnsureCanAccess(appointment.BranchId);
            return AppointmentAudience.Staff;
        }

        if (currentUser.CustomerId is { Length: > 0 } customerId
            && await petAccess.IsCaredForByAsync(appointment.PetId, customerId, cancellationToken))
        {
            return AppointmentAudience.Customer;
        }

        throw new NotFoundException("appointment", appointment.Id);
    }
}
