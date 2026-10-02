using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Scheduling;

/// <summary>
/// Leave: a staff member away for a stretch of time — <c>POST /staff/schedules/time-off</c>,
/// "ลา · ระบบเตือนนัดที่ชนและเสนอหมอแทน".
/// <para>
/// Recording it takes the time out of every shift it touches (see <see cref="StaffShift.CarveOut"/>),
/// which is what stops new bookings landing in it. Appointments already booked inside it are not
/// moved or cancelled: the use case reports them with the colleagues free to take each one, and a
/// person decides.
/// </para>
/// </summary>
public sealed class StaffTimeOff : Entity
{
    public const int ReasonMaxLength = 200;

    /// <summary>Longer than this is not leave but a change of employment, and belongs on the staff record.</summary>
    public static readonly TimeSpan LongestStretch = TimeSpan.FromDays(90);

    private StaffTimeOff()
    {
    }

    public string StaffId { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Free text, e.g. "ลาป่วย". Shown to staff, never to customers.</summary>
    public string? Reason { get; private set; }

    /// <summary>Who recorded it; null when recorded by an operator command.</summary>
    public string? RecordedByStaffId { get; private set; }

    public static StaffTimeOff Record(
        string staffId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? reason,
        string? recordedByStaffId)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("invalid_time_off", "เวลาสิ้นสุดการลาต้องอยู่หลังเวลาเริ่ม");
        }

        if (endsAt - startsAt > LongestStretch)
        {
            throw new DomainException("invalid_time_off", $"ลาได้ครั้งละไม่เกิน {LongestStretch.Days} วัน");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (trimmed is { Length: > ReasonMaxLength })
        {
            throw new DomainException("invalid_time_off", $"เหตุผลยาวได้ไม่เกิน {ReasonMaxLength} ตัวอักษร");
        }

        return new StaffTimeOff
        {
            Id = EntityId.New(EntityId.TimeOff),
            StaffId = staffId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = trimmed,
            RecordedByStaffId = recordedByStaffId,
        };
    }

    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        StartsAt < endsAt && startsAt < EndsAt;
}
