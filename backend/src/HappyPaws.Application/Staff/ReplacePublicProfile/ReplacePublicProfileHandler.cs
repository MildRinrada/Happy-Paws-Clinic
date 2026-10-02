using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.ReplacePublicProfile;

/// <summary>
/// <c>PUT /staff/members/{id}/public-profile</c>. Replaces the whole profile: a field left out is
/// cleared, not kept. Extension, § 10.
/// </summary>
/// <param name="StaffId">The member whose profile changes.</param>
/// <param name="Profile">The new profile.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record ReplacePublicProfileCommand(
    string StaffId,
    StaffPublicProfile Profile,
    uint? IfMatchVersion = null);

/// <summary>
/// Sets what the public team page says about a member, and whether it lists them.
/// <para>
/// Held to the same rules as any other change to a member (<see cref="StaffMemberAccess"/>): a
/// branch-bound manager edits people at their branches, and nobody edits someone who holds more
/// than they do. Putting a person's name and face on the website is a change to that person.
/// </para>
/// </summary>
public sealed class ReplacePublicProfileHandler(
    StaffMemberAccess access,
    IStaffRepository staff,
    IRoleRepository roles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<StaffMemberDto> HandleAsync(
        ReplacePublicProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (membership, _) = await access.ForChangeAsync(command.StaffId, token);

            ConcurrencyGuard.Ensure(membership, command.IfMatchVersion);

            var before = membership.PublicProfile;

            membership.ReplacePublicProfile(command.Profile, clock.NowInClinicTime().Year);

            audit.Record(new AuditEvent(
                AuditActions.StaffPublicProfileUpdated,
                AuditResources.StaffMember,
                membership.Id,
                Details: new Dictionary<string, object?>
                {
                    ["before"] = Describe(before),
                    ["after"] = Describe(membership.PublicProfile),
                }));
        }, cancellationToken);

        var row = await staff.FindRowAsync(command.StaffId, cancellationToken)
            ?? throw new NotFoundException("staff_member", command.StaffId);

        var roleNames = (await roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id, role => role.Name);

        return StaffMemberDto.From(row, roleNames);
    }

    private static Dictionary<string, object?> Describe(StaffPublicProfile profile) => new()
    {
        ["job_title"] = profile.JobTitle,
        ["specialty"] = profile.Specialty,
        ["bio"] = profile.Bio,
        ["photo_path"] = profile.PhotoPath,
        ["career_start_year"] = profile.CareerStartYear,
        ["show_on_website"] = profile.ShowOnWebsite,
    };
}
