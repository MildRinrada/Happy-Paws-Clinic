using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// A day the clinic is shut — <c>POST /clinic/holidays</c>, "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ".
/// <para>
/// A plain calendar date in clinic-local terms, like a queue's service date: Songkran is the 13th
/// in Bangkok whatever offset the caller's browser reports.
/// </para>
/// <para>
/// "ปิดรับนัดอัตโนมัติ" is the behaviour, and it is enforced where slots are computed rather than
/// remembered by each booking path: the availability feed offers nothing on a holiday and the slot
/// resolver refuses one. Appointments already in the calendar are left alone — closing the clinic
/// for a day is a decision about new bookings, and cancelling existing ones is a conversation the
/// counter has with each owner.
/// </para>
/// </summary>
public sealed class ClinicHoliday : Entity
{
    private ClinicHoliday()
    {
    }

    public DateOnly Date { get; private set; }

    /// <summary>What the day is — "วันสงกรานต์", "ปิดปรับปรุง". Shown to customers.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The one branch that is shut. Null closes every branch.</summary>
    public string? BranchId { get; private set; }

    public static ClinicHoliday Declare(DateOnly date, string name, string? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_holiday", "ต้องระบุชื่อวันหยุด");
        }

        return new ClinicHoliday
        {
            Id = EntityId.New(EntityId.Holiday),
            Date = date,
            Name = name.Trim(),
            BranchId = string.IsNullOrWhiteSpace(branchId) ? null : branchId,
        };
    }

    /// <summary>True when this holiday shuts the given branch.</summary>
    public bool Closes(string branchId) => BranchId is null || BranchId == branchId;
}
