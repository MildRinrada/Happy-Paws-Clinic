using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Appointments;

/// <summary>
/// The clinic's rule for late cancellations and no-shows — <c>PUT /staff/policies/cancellation</c>,
/// "cutoff ค่าปรับ และจำนวนครั้งที่ยอมให้".
/// <para>
/// The three things that line names, and one it needs: a window, because "how many times" means
/// nothing without "within how long". A customer who missed one visit three years ago and one last
/// week has not missed two in a row.
/// </para>
/// <para>
/// <b>Not the same cutoff as the booking settings'.</b> That one decides whether a customer may
/// still <i>move</i> an appointment themselves. This one decides whether a cancellation
/// <i>counts</i>. Cancelling is never blocked; it is only ever counted. See
/// <c>docs/assumptions.md § 15</c> and § 37.
/// </para>
/// <para>
/// The fee is worked out and reported, never charged on its own. Billing a customer is a decision
/// for the counter, made with the customer in front of them, through the invoice endpoints.
/// </para>
/// </summary>
public sealed class CancellationPolicy : Entity
{
    public const string SingletonId = "set_cancellation_policy";

    private CancellationPolicy()
    {
    }

    /// <summary>A customer cancellation less than this many hours before the visit is late.</summary>
    public int CutoffHours { get; private set; } = 24;

    /// <summary>What each strike past the allowance costs, in satang. Zero for a clinic that only counts.</summary>
    public long FeeSatang { get; private set; }

    /// <summary>How many late cancellations and no-shows, together, are forgiven within the window.</summary>
    public int FreeAllowance { get; private set; } = 1;

    /// <summary>How far back strikes are counted.</summary>
    public int WindowDays { get; private set; } = 365;

    public static CancellationPolicy CreateDefault() => new() { Id = SingletonId };

    public void Replace(int cutoffHours, long feeSatang, int freeAllowance, int windowDays)
    {
        if (cutoffHours is < 0 or > 168)
        {
            throw Invalid("cutoff ต้องอยู่ระหว่าง 0 ถึง 168 ชั่วโมง");
        }

        if (feeSatang is < 0 or > 10_000_000)
        {
            throw Invalid("ค่าปรับต้องไม่ติดลบและไม่เกิน 100,000 บาท");
        }

        if (freeAllowance is < 0 or > 100)
        {
            throw Invalid("จำนวนครั้งที่ยอมให้ต้องอยู่ระหว่าง 0 ถึง 100");
        }

        if (windowDays is < 1 or > 730)
        {
            throw Invalid("ช่วงเวลาที่นับต้องอยู่ระหว่าง 1 ถึง 730 วัน");
        }

        CutoffHours = cutoffHours;
        FeeSatang = feeSatang;
        FreeAllowance = freeAllowance;
        WindowDays = windowDays;
    }

    /// <summary>Whether cancelling now, for a visit starting then, is inside the cutoff.</summary>
    public bool IsLate(DateTimeOffset now, DateTimeOffset startsAt) => startsAt - now < TimeSpan.FromHours(CutoffHours);

    /// <summary>The fee the latest strike carries: nothing until the allowance is used up.</summary>
    public long FeeFor(int strikesInWindow) => strikesInWindow > FreeAllowance ? FeeSatang : 0;

    private static DomainException Invalid(string message) => new("invalid_cancellation_policy", message);
}
