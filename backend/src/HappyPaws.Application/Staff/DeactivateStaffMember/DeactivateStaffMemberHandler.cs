using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Staff.DeactivateStaffMember;

/// <summary>
/// <c>POST /staff/members/{id}/deactivate</c> — "ปิดสิทธิ์ทันที แต่เก็บชื่อในเวชระเบียนเดิม".
/// </summary>
public sealed record DeactivateStaffMemberCommand(string StaffId);

/// <summary>
/// Ends someone's employment in the system.
/// <para>
/// <b>"เก็บชื่อในเวชระเบียนเดิม" is the constraint that shapes this.</b> Nothing is deleted: the
/// membership row stays and the account stays, because every record this person signed points at
/// them and a signed record with no signatory is not a record. What changes is that the membership
/// stops granting anything.
/// </para>
/// <para>
/// <b>"ปิดสิทธิ์ทันที" has a fifteen-minute edge, and it is stated rather than hidden.</b> Every
/// session is revoked here, so no new access token can be minted — sign-in and refresh both check
/// the membership. An access token already issued stays valid until it expires, which is at most
/// fifteen minutes; that is the window this system accepts for every token-carried fact
/// (<c>docs/authentication.md § 3</c>).
/// </para>
/// <para>
/// Deactivating an already-deactivated member succeeds and changes nothing, since the usual reason
/// to press twice is that the first response was lost. Deactivating yourself is refused: the
/// account that pressed the button would be the one locked out by it.
/// </para>
/// </summary>
public sealed class DeactivateStaffMemberHandler(
    StaffMemberAccess access,
    OwnerLockoutGuard lockout,
    IStaffRepository staff,
    IRoleRepository roles,
    IIdentityRepository identity,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock)
{
    public async Task<StaffMemberDto> HandleAsync(
        DeactivateStaffMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.StaffId == currentUser.StaffId)
        {
            throw new CannotDeactivateSelfException();
        }

        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (membership, _) = await access.ForChangeAsync(command.StaffId, token);

            if (!membership.IsActive)
            {
                return;
            }

            var ownerCheck = await lockout.BeforeChangeAsync(token);

            membership.Deactivate();

            // No session is excepted. RevokeOtherSessions with no session to keep is "revoke all".
            await identity.RevokeOtherSessionsAsync(membership.UserId, string.Empty, now, token);

            audit.Record(new AuditEvent(
                AuditActions.StaffDeactivated,
                AuditResources.StaffMember,
                membership.Id,
                Details: new Dictionary<string, object?> { ["user_id"] = membership.UserId }));

            await ownerCheck.EnsureAnOwnerRemainsAsync(token);
        }, cancellationToken);

        var row = await staff.FindRowAsync(command.StaffId, cancellationToken)
            ?? throw new NotFoundException("staff_member", command.StaffId);

        var roleNames = (await roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id, role => role.Name);

        return StaffMemberDto.From(row, roleNames, storage);
    }
}

/// <summary><c>409 cannot_deactivate_self</c>. Not in the API reference's error list.</summary>
public sealed class CannotDeactivateSelfException : AppException
{
    public CannotDeactivateSelfException()
        : base("cannot_deactivate_self", "ปิดใช้งานบัญชีของตัวเองไม่ได้", HttpStatus.Conflict)
    {
        MessageEn = "You cannot deactivate your own staff membership.";
    }
}
