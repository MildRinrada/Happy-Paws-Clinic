using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Scheduling;

/// <summary>
/// One rule: cases matching it go to these people first, in this order.
/// </summary>
/// <param name="Species">Matches a pet's species, compared without case, e.g. <c>cat</c>; null for any.</param>
/// <param name="ServiceId">Matches one service; null for any.</param>
/// <param name="StaffIds">Who is preferred, most preferred first.</param>
public sealed record AssignmentRule(string? Species, string? ServiceId, IReadOnlyList<string> StaffIds)
{
    public bool Matches(string? serviceId, string? species) =>
        (ServiceId is null || ServiceId == serviceId)
        && (Species is null || string.Equals(Species, species?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The clinic's rules for handing out cases automatically — <c>PUT /staff/schedules/assignment-rules</c>,
/// "กติกาจ่ายเคสอัตโนมัติ เช่น หมอแมวรับเคสแมวก่อน".
/// <para>
/// <b>A preference, never a restriction.</b> "หมอแมวรับเคสแมวก่อน" says the cat vet takes cat cases
/// first, not only. When nobody a rule names is free, the case goes to whoever is, exactly as if
/// there were no rules; a rule must never be the reason a free slot cannot be booked.
/// </para>
/// <para>
/// The first matching rule wins, so a clinic puts its narrow rules (one service for cats) above
/// its broad ones (any cat). Who may perform a service at all is still the service's own
/// provider list; a rule only orders the people already allowed.
/// </para>
/// <para>
/// One row, written whole, like the clinic's other settings.
/// </para>
/// </summary>
public sealed class AssignmentRules : Entity
{
    public const string SingletonId = "set_assignment";
    public const int MaxRules = 50;
    public const int MaxStaffPerRule = 20;
    public const int SpeciesMaxLength = 40;

    private List<AssignmentRule> _rules = [];

    private AssignmentRules()
    {
    }

    public IReadOnlyList<AssignmentRule> Rules => _rules;

    public static AssignmentRules CreateDefault() => new() { Id = SingletonId };

    /// <summary>Replaces every rule. Checking that the staff and services exist is the use case's job.</summary>
    public void Replace(IReadOnlyList<AssignmentRule> rules)
    {
        if (rules.Count > MaxRules)
        {
            throw Invalid($"มีกติกาได้ไม่เกิน {MaxRules} ข้อ");
        }

        var cleaned = new List<AssignmentRule>(rules.Count);

        foreach (var rule in rules)
        {
            var species = string.IsNullOrWhiteSpace(rule.Species) ? null : rule.Species.Trim().ToLowerInvariant();
            var serviceId = string.IsNullOrWhiteSpace(rule.ServiceId) ? null : rule.ServiceId.Trim();

            if (species is null && serviceId is null)
            {
                // A rule for every case is a fixed order of vets, which is not what the contract describes.
                throw Invalid("แต่ละกติกาต้องระบุชนิดสัตว์ บริการ หรือทั้งสองอย่าง");
            }

            if (species is { Length: > SpeciesMaxLength })
            {
                throw Invalid($"ชนิดสัตว์ยาวได้ไม่เกิน {SpeciesMaxLength} ตัวอักษร");
            }

            var staffIds = rule.StaffIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (staffIds.Length is 0 or > MaxStaffPerRule)
            {
                throw Invalid($"แต่ละกติกาต้องระบุผู้รับเคส 1 ถึง {MaxStaffPerRule} คน");
            }

            cleaned.Add(new AssignmentRule(species, serviceId, staffIds));
        }

        _rules = cleaned;
    }

    /// <summary>
    /// Ranks candidates for a case: those the first matching rule names come first in its order,
    /// then everyone else in a stable order.
    /// </summary>
    /// <param name="candidates">Staff already allowed and free for the case.</param>
    /// <param name="serviceId">The case's service.</param>
    /// <param name="species">The pet's species, or null when not known yet.</param>
    public IReadOnlyList<string> Rank(IEnumerable<string> candidates, string? serviceId, string? species)
    {
        var preferred = _rules.FirstOrDefault(rule => rule.Matches(serviceId, species))?.StaffIds ?? [];

        return
        [
            .. candidates
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => preferred.Contains(id) ? IndexOf(preferred, id) : int.MaxValue)
                .ThenBy(id => id, StringComparer.Ordinal),
        ];
    }

    private static int IndexOf(IReadOnlyList<string> list, string value)
    {
        for (var index = 0; index < list.Count; index++)
        {
            if (list[index] == value)
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static DomainException Invalid(string message) => new("invalid_assignment_rules", message);
}
