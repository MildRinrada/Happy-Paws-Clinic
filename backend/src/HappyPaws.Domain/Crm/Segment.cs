using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// The conditions of a customer group. Every condition given must hold (AND); a condition left
/// out does not narrow the group.
/// </summary>
/// <param name="Species">The customer has at least one living pet of one of these species (case-insensitive).</param>
/// <param name="InactiveDays">"ไม่มา N วัน": has visited before, and the last visit was more than N days ago.</param>
/// <param name="VisitedWithinDays">Has visited in the last N days.</param>
/// <param name="MinSpendSatang">Paid less refunded over the last 365 days is at least this.</param>
/// <param name="Tiers">The customer's loyalty level is one of these.</param>
/// <param name="HasActivePackage">True: holds a package that can be used today. False: holds none.</param>
/// <param name="BranchId">Only customers who have visited this branch; visits and spend count only there.</param>
public sealed record SegmentCriteria(
    IReadOnlyList<string> Species,
    int? InactiveDays,
    int? VisitedWithinDays,
    long? MinSpendSatang,
    IReadOnlyList<LoyaltyTier> Tiers,
    bool? HasActivePackage,
    string? BranchId)
{
    /// <summary>How far back "spend" looks.</summary>
    public const int SpendWindowDays = 365;

    /// <summary>A group with no condition would be every customer; that is not a segment.</summary>
    public bool IsEmpty =>
        Species.Count == 0 && InactiveDays is null && VisitedWithinDays is null && MinSpendSatang is null
        && Tiers.Count == 0 && HasActivePackage is null && string.IsNullOrWhiteSpace(BranchId);
}

/// <summary>
/// A saved customer group — <c>POST /staff/segments</c>, "กลุ่มลูกค้าจากเงื่อนไข เช่น ไม่มา 6 เดือน".
/// <para>
/// <b>Stored as conditions, not as a list of people.</b> Who is in "not seen for six months" changes
/// every day, so the members are worked out again whenever the group is used (a LINE broadcast with
/// <c>segment_id</c>). The counts kept here are the ones taken when it was created, for the record.
/// </para>
/// </summary>
public sealed class Segment : Entity
{
    private readonly List<string> _species = [];
    private readonly List<LoyaltyTier> _tiers = [];

    private Segment()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<string> Species => _species;

    public int? InactiveDays { get; private set; }

    public int? VisitedWithinDays { get; private set; }

    public long? MinSpendSatang { get; private set; }

    public IReadOnlyList<LoyaltyTier> Tiers => _tiers;

    public bool? HasActivePackage { get; private set; }

    public string? BranchId { get; private set; }

    public string CreatedByStaffId { get; private set; } = string.Empty;

    /// <summary>Members when the group was created.</summary>
    public int CustomerCount { get; private set; }

    /// <summary>Of those, how many a LINE broadcast could reach.</summary>
    public int ReachableCount { get; private set; }

    public SegmentCriteria Criteria =>
        new([.. _species], InactiveDays, VisitedWithinDays, MinSpendSatang, [.. _tiers], HasActivePackage, BranchId);

    public static Segment Create(string name, SegmentCriteria criteria, string createdByStaffId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_segment", "กลุ่มลูกค้าต้องมีชื่อ");
        }

        if (criteria.IsEmpty)
        {
            throw new DomainException("invalid_segment", "ต้องมีเงื่อนไขอย่างน้อยหนึ่งข้อ");
        }

        if (criteria is { InactiveDays: { } inactive, VisitedWithinDays: { } within } && within <= inactive)
        {
            throw new DomainException("invalid_segment", "จำนวนวันที่มาล่าสุดต้องมากกว่าจำนวนวันที่ไม่มา");
        }

        var segment = new Segment
        {
            Id = EntityId.New(EntityId.Segment),
            Name = name.Trim(),
            InactiveDays = criteria.InactiveDays,
            VisitedWithinDays = criteria.VisitedWithinDays,
            MinSpendSatang = criteria.MinSpendSatang,
            HasActivePackage = criteria.HasActivePackage,
            BranchId = string.IsNullOrWhiteSpace(criteria.BranchId) ? null : criteria.BranchId,
            CreatedByStaffId = createdByStaffId,
        };

        segment._species.AddRange(criteria.Species
            .Select(species => species.Trim())
            .Where(species => species.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));
        segment._tiers.AddRange(criteria.Tiers.Distinct());

        return segment;
    }

    public void RecordCounts(int customerCount, int reachableCount)
    {
        CustomerCount = customerCount;
        ReachableCount = reachableCount;
    }
}
