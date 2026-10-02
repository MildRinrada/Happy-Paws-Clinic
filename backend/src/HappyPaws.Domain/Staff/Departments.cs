namespace HappyPaws.Domain.Staff;

/// <summary>One department: its code on the wire and the name a person reads.</summary>
/// <param name="Code">Stable, lower-case code, e.g. <c>veterinary</c>.</param>
/// <param name="Name">Thai name, e.g. <c>ทีมสัตวแพทย์</c>.</param>
public sealed record Department(string Code, string Name);

/// <summary>
/// The teams a member of staff belongs to, for grouping people: on the public team page and in
/// the staff list. <b>Contract extension</b>, see <c>docs/contract-extensions.md § 10</c>.
/// <para>
/// A department is not a role. A role is what someone may do in the system and is checked on every
/// request; a department is which team they are in and is never checked at all. The two usually
/// line up (vets hold <c>veterinarian</c>) but need not: a head nurse may be given more scopes
/// without leaving nursing, and a cleaner is in a department while holding no role.
/// </para>
/// <para>
/// A fixed list rather than clinic-defined, unlike <see cref="StaffMembership.StaffType"/>. The
/// public page needs a known set it can order and describe, and six teams cover a veterinary
/// clinic of any size.
/// </para>
/// </summary>
public static class Departments
{
    public const string Veterinary = "veterinary";
    public const string Nursing = "nursing";
    public const string FrontDesk = "front_desk";
    public const string Finance = "finance";
    public const string Management = "management";
    public const string Support = "support";

    /// <summary>Every department, in the order the team page shows them.</summary>
    public static readonly IReadOnlyList<Department> All =
    [
        new(Veterinary, "ทีมสัตวแพทย์"),
        new(Nursing, "ผู้ช่วยสัตวแพทย์"),
        new(FrontDesk, "ต้อนรับและนัดหมาย"),
        new(Finance, "การเงิน"),
        new(Management, "บริหารคลินิก"),
        new(Support, "ทีมสนับสนุน"),
    ];

    public static bool IsKnown(string? code) => All.Any(department => department.Code == code);

    /// <summary>Position in <see cref="All"/>, for ordering; unknown codes sort last.</summary>
    public static int OrderOf(string code)
    {
        for (var index = 0; index < All.Count; index++)
        {
            if (All[index].Code == code)
            {
                return index;
            }
        }

        return All.Count;
    }

    public static string NameOf(string code) =>
        All.FirstOrDefault(department => department.Code == code)?.Name ?? code;

    /// <summary>
    /// Staff types with an obvious department. Any other staff type starts in <see cref="Support"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> StaffTypeDefaults = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["veterinarian"] = Veterinary,
        ["vet_assistant"] = Nursing,
        ["receptionist"] = FrontDesk,
        ["cashier"] = Finance,
        ["owner"] = Management,
        ["clinic_admin"] = Management,
    };

    /// <summary>
    /// The department a staff type most likely belongs to. Used when a membership is made without
    /// one: from an invitation, which carries only a staff type, and by bootstrap. The clinic can
    /// move anyone afterwards.
    /// </summary>
    public static string ForStaffType(string? staffType) =>
        staffType is not null && StaffTypeDefaults.TryGetValue(staffType.Trim(), out var department)
            ? department
            : Support;
}
