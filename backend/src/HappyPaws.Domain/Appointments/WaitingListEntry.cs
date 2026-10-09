using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Appointments;

/// <summary>
/// Someone waiting for a slot to open — <c>POST /staff/waiting-list</c>, "คิวรอคิวว่าง · ยิง LINE
/// อัตโนมัติเมื่อมีคนยกเลิก".
/// <para>
/// An entry is a standing request: this pet, this service, any day between two dates, optionally at
/// one branch or with one vet. When an appointment that fits is cancelled, the people waiting are
/// told. Being told is not a booking. The freed slot goes to whoever books it first, through the
/// ordinary booking path, so a waiting list can never double-book a vet.
/// </para>
/// <para>
/// There is no status to manage. An entry lapses on its own once its last date has passed, and
/// withdrawing one (<c>DELETE /staff/waiting-list/{id}</c>, <c>docs/contract-extensions.md § 23</c>)
/// deletes the row, so a field that could only ever say "waiting" would be decoration. The list
/// works out <c>waiting</c> or <c>lapsed</c> from <see cref="IsLapsed"/>.
/// </para>
/// </summary>
public sealed class WaitingListEntry : Entity
{
    /// <summary>A waiting list is for the near future; a year out is a booking, not a wait.</summary>
    public const int MaxWindowDays = 90;

    private WaitingListEntry()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string PetId { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Only this branch, or null for any.</summary>
    public string? BranchId { get; private set; }

    /// <summary>Only this vet, or null for any.</summary>
    public string? StaffId { get; private set; }

    /// <summary>The first clinic day that would do.</summary>
    public DateOnly EarliestOn { get; private set; }

    /// <summary>The last clinic day that would do. The entry lapses after it.</summary>
    public DateOnly LatestOn { get; private set; }

    /// <summary>"เช้าเท่านั้น", "หลังเลิกงาน" — for the person who rings back.</summary>
    public string? Note { get; private set; }

    public string AddedByStaffId { get; private set; } = string.Empty;

    /// <summary>When this entry was last told about an opening, or null if never.</summary>
    public DateTimeOffset? LastNotifiedAt { get; private set; }

    public int NotifiedCount { get; private set; }

    public static WaitingListEntry Add(
        string customerId,
        string petId,
        string serviceId,
        DateOnly earliestOn,
        DateOnly latestOn,
        DateOnly today,
        string addedByStaffId,
        string? branchId = null,
        string? staffId = null,
        string? note = null)
    {
        if (earliestOn > latestOn)
        {
            throw Invalid("วันแรกที่สะดวกต้องไม่อยู่หลังวันสุดท้าย");
        }

        if (latestOn < today)
        {
            throw Invalid("ช่วงวันที่รอต้องยังไม่ผ่านไป");
        }

        if (latestOn.DayNumber - today.DayNumber > MaxWindowDays)
        {
            throw Invalid($"รอคิวว่างได้ไม่เกิน {MaxWindowDays} วันข้างหน้า");
        }

        return new WaitingListEntry
        {
            Id = EntityId.New(EntityId.WaitingList),
            CustomerId = customerId,
            PetId = petId,
            ServiceId = serviceId,
            BranchId = branchId,
            StaffId = staffId,
            EarliestOn = earliestOn < today ? today : earliestOn,
            LatestOn = latestOn,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            AddedByStaffId = addedByStaffId,
        };
    }

    public bool IsLapsed(DateOnly today) => today > LatestOn;

    /// <summary>
    /// Whether a freed slot is one this entry would take: same service, a day in the window, and
    /// the branch and vet if the entry named them. Never the pet whose visit was just cancelled.
    /// </summary>
    public bool Fits(Appointment freed, DateOnly freedOn) =>
        freed.ServiceId == ServiceId
        && freed.PetId != PetId
        && freedOn >= EarliestOn
        && freedOn <= LatestOn
        && (BranchId is null || BranchId == freed.BranchId)
        && (StaffId is null || StaffId == freed.StaffId);

    public void MarkNotified(DateTimeOffset at)
    {
        LastNotifiedAt = at;
        NotifiedCount++;
    }

    private static DomainException Invalid(string message) => new("invalid_waiting_list", message);
}
