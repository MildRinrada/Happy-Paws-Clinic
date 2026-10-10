using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Application.Staff.Photos;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.ListTeam;

/// <summary><c>GET /team</c>. Extension, § 10.</summary>
/// <param name="Department">Only this department, a code from <see cref="Departments"/>; null for all.</param>
public sealed record ListTeamQuery(string? Department = null);

/// <summary>
/// The clinic's people as the public website shows them, grouped by department.
/// <para>
/// Only members who are active, whose account can sign in, and who have been set to show on the
/// website. Being on the staff is not consent to be on the website, so nobody appears until their
/// public profile says so.
/// </para>
/// <para>
/// Departments come in the fixed order of <see cref="Departments.All"/>, and one with nobody
/// listed is left out rather than shown empty. Inside a department the most experienced come
/// first, then by name; people with no start year come last.
/// </para>
/// <para>
/// An uploaded photo (§ 32) is answered as a signed link minted for this read, and only ever for
/// someone this list shows: the storage value itself never leaves, and a hidden member gets no
/// link at all.
/// </para>
/// </summary>
public sealed class ListTeamHandler(IStaffRepository staff, IFileStorage storage, IClock clock)
{
    public async Task<IReadOnlyList<TeamDepartmentDto>> HandleAsync(
        ListTeamQuery query,
        CancellationToken cancellationToken = default)
    {
        var department = query.Department?.Trim().ToLowerInvariant();

        if (department is { Length: > 0 } && !Departments.IsKnown(department))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["department"] =
                [
                    $"แผนกต้องเป็นหนึ่งใน {string.Join(", ", Departments.All.Select(known => known.Code))}",
                ],
            });
        }

        var rows = await staff.ListOnWebsiteAsync(department is { Length: > 0 } ? department : null, cancellationToken);
        var year = clock.NowInClinicTime().Year;

        return
        [
            .. rows
                .GroupBy(row => row.Membership.Department)
                .OrderBy(group => Departments.OrderOf(group.Key))
                .Select(group => new TeamDepartmentDto(
                    group.Key,
                    Departments.NameOf(group.Key),
                    [
                        .. group
                            .OrderBy(row => row.Membership.CareerStartYear ?? int.MaxValue)
                            .ThenBy(row => row.Account.DisplayName, StringComparer.Ordinal)
                            .Select(row => new TeamMemberDto(
                                row.Membership.Id,
                                row.Account.DisplayName,
                                row.Membership.JobTitle,
                                row.Membership.Specialty,
                                row.Membership.Bio,
                                StaffPhoto.SitePathOf(row.Membership.PhotoPath),
                                row.Membership.YearsOfExperience(year),
                                StaffPhotoLinks.LinkOf(row.Membership.PhotoPath, storage, StaffPhotoLinks.PublicLinkLifetime))),
                    ])),
        ];
    }
}
