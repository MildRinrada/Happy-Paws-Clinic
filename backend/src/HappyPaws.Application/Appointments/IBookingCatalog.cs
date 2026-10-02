using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// Read port for everything booking needs but does not own: the clinic's booking rules, the
/// service catalogue, branches and the shift roster.
/// <para>
/// Grouped into one port on purpose. These are four small reads that the booking use cases always
/// perform together, and four separate interfaces would mean four constructor parameters on every
/// handler for no gain in substitutability. When the Clinic-setup, Services and Scheduling modules
/// arrive with their own write endpoints, they bring their own repositories; this stays the
/// booking-shaped view of their data.
/// </para>
/// </summary>
public interface IBookingCatalog
{
    /// <summary>The instance's booking rules. Never null — a default row is seeded on migration.</summary>
    Task<BookingSettings> GetBookingSettingsAsync(CancellationToken cancellationToken = default);

    Task<ClinicService?> FindServiceAsync(string serviceId, CancellationToken cancellationToken = default);

    Task<Branch?> FindBranchAsync(string branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active shifts overlapping a window, optionally narrowed to one vet. This is what makes a
    /// time bookable at all: "ตารางเวรเป็นตัวกำหนด slot ที่ /booking/availability คำนวณ".
    /// </summary>
    Task<IReadOnlyList<StaffShift>> ShiftsAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? staffId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The holiday that shuts this branch on a clinic-local day, or null when it is open — what
    /// makes "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ" automatic rather than something each booking path
    /// has to remember.
    /// </summary>
    Task<ClinicHoliday?> HolidayAsync(
        string branchId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The single branch of a one-site clinic, used when the caller omits <c>?branch=</c>. Null
    /// when the clinic has several, in which case the caller must say which one.
    /// </summary>
    Task<Branch?> SoleActiveBranchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The clinic's rules for choosing a vet when a booking names none. Never null: a clinic that
    /// has written none gets an empty set, which leaves the stable default order.
    /// </summary>
    Task<AssignmentRules> AssignmentRulesAsync(CancellationToken cancellationToken = default);
}
