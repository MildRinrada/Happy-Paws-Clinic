namespace HappyPaws.Application.Staff.Dtos;

/// <summary>
/// One department on the public team page — <c>GET /team</c>. Extension, § 10.
/// </summary>
/// <param name="Department">The department code, e.g. <c>veterinary</c>.</param>
/// <param name="Name">The Thai name, e.g. <c>ทีมสัตวแพทย์</c>.</param>
/// <param name="Members">Who is in it, most experienced first.</param>
public sealed record TeamDepartmentDto(string Department, string Name, IReadOnlyList<TeamMemberDto> Members);

/// <summary>
/// One person as the public sees them. Deliberately short: no email, phone, roles, branches,
/// licence or account details, whatever the staff list holds.
/// </summary>
/// <param name="Id">Staff id, the same one booking availability returns for a vet.</param>
/// <param name="DisplayName">Name with title, e.g. <c>สพ.ญ. มินท์ ศรีสุข</c>.</param>
/// <param name="JobTitle">Title as the public reads it, or null.</param>
/// <param name="Specialty">Field of work, or null.</param>
/// <param name="Bio">A few sentences, or null.</param>
/// <param name="PhotoPath">Site-relative photo, or null.</param>
/// <param name="YearsOfExperience">Whole years since they started, or null when unknown.</param>
public sealed record TeamMemberDto(
    string Id,
    string DisplayName,
    string? JobTitle,
    string? Specialty,
    string? Bio,
    string? PhotoPath,
    int? YearsOfExperience);
