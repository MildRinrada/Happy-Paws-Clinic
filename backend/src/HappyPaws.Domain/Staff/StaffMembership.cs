using HappyPaws.Domain.Common;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Domain.Staff;

/// <summary>
/// An account's employment in this clinic: which roles it holds and which branches it works at.
/// <para>
/// Separate from <see cref="UserAccount"/> because the same person may also be a customer here,
/// and because deactivating a staff member must not erase the account whose name appears on signed
/// medical records.
/// </para>
/// </summary>
public sealed partial class StaffMembership : Entity
{
    private readonly List<string> _roleIds = [];
    private readonly List<string> _branchIds = [];

    private StaffMembership()
    {
    }

    public string UserId { get; private set; } = string.Empty;

    /// <summary>Clinic-defined staff type, e.g. <c>veterinarian</c> — a label, never an access decision.</summary>
    public string StaffType { get; private set; } = string.Empty;

    /// <summary>Professional licence number, e.g. <c>ส.พ. 06120</c>. Required for vets.</summary>
    public string? LicenseNo { get; private set; }

    /// <summary>Which team they are in, a code from <see cref="Departments"/>. A grouping, never an access decision.</summary>
    public string Department { get; private set; } = Departments.Support;

    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<string> RoleIds => _roleIds;

    public IReadOnlyList<string> BranchIds => _branchIds;

    public static StaffMembership Create(
        string userId,
        string staffType,
        IEnumerable<string> roleIds,
        IEnumerable<string> branchIds,
        string? licenseNo = null,
        string? department = null)
    {
        var membership = new StaffMembership
        {
            Id = EntityId.New(EntityId.Staff),
            UserId = userId,
            StaffType = staffType,
            LicenseNo = licenseNo,
            Department = Departments.ForStaffType(staffType),
        };

        if (department is not null)
        {
            membership.MoveToDepartment(department);
        }

        membership._roleIds.AddRange(roleIds.Distinct(StringComparer.Ordinal));
        membership._branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal));

        return membership;
    }

    /// <summary>
    /// Ends the membership. The row is kept, per the contract: "ปิดสิทธิ์ทันที แต่เก็บชื่อในเวชระเบียนเดิม".
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// <c>PATCH /staff/members/{id}</c> — "ตำแหน่ง สาขา เลขใบประกอบวิชาชีพ". A null argument leaves
    /// that field alone; an empty licence number clears it.
    /// </summary>
    public void UpdateProfile(string? staffType = null, string? licenseNo = null)
    {
        if (staffType is not null)
        {
            if (string.IsNullOrWhiteSpace(staffType))
            {
                throw new DomainException("invalid_staff_member", "ต้องระบุตำแหน่ง");
            }

            StaffType = staffType.Trim();
        }

        if (licenseNo is not null)
        {
            LicenseNo = string.IsNullOrWhiteSpace(licenseNo) ? null : licenseNo.Trim();
        }
    }

    /// <summary>Moves the member to another team. Changes nothing they can do.</summary>
    public void MoveToDepartment(string department)
    {
        var code = department.Trim();

        if (!Departments.IsKnown(code))
        {
            throw new DomainException(
                "invalid_department",
                $"แผนกต้องเป็นหนึ่งใน {string.Join(", ", Departments.All.Select(known => known.Code))}");
        }

        Department = code;
    }

    public void ReplaceRoles(IEnumerable<string> roleIds)
    {
        _roleIds.Clear();
        _roleIds.AddRange(roleIds.Distinct(StringComparer.Ordinal));
    }

    public void ReplaceBranches(IEnumerable<string> branchIds)
    {
        _branchIds.Clear();
        _branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal));
    }

    /// <summary>
    /// A vet, for the purpose of putting them on a queue ticket: the <c>veterinarian</c> staff type,
    /// or anyone in the veterinary department. The same rule the queue board's vet picker has always
    /// used, now also checked by <c>POST /staff/queue/{id}/assign</c> (<c>docs/contract-extensions.md § 31</c>).
    /// </summary>
    public bool IsVeterinarian =>
        string.Equals(StaffType, "veterinarian", StringComparison.Ordinal)
        || string.Equals(Department, Departments.Veterinary, StringComparison.Ordinal);

    /// <summary>Works at this branch: assigned to it, or bound to no branch at all.</summary>
    public bool WorksAt(string branchId) => _branchIds.Count == 0 || _branchIds.Contains(branchId);
}
