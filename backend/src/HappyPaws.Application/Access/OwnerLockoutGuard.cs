using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff;

namespace HappyPaws.Application.Access;

/// <summary>
/// Keeps a clinic from locking itself out of its own instance.
/// <para>
/// There is no platform admin to call ("ไม่มี platform admin") — if the last account holding
/// <c>*</c> is deactivated, demoted, or has <c>*</c> edited out of its role, nobody left can undo
/// it and the only way back in is the server's command line. Three different endpoints can cause
/// that, so the rule lives here once instead of being remembered in each.
/// </para>
/// <para>
/// The rule is about <b>this change</b>: it is refused if an owner existed before it and none would
/// after. That is asked in two halves, either side of the mutation, inside its transaction — the
/// pending writes are flushed, the question is put to the database as it would stand, and a "no"
/// rolls everything back. Predicting the answer beforehand would mean working out the effect of a
/// role edit on every member who holds that role, which is the query anyway.
/// </para>
/// </summary>
public sealed class OwnerLockoutGuard(IStaffRepository staff, IUnitOfWork unitOfWork)
{
    /// <summary>Call before changing a role or a membership, inside the transaction.</summary>
    public async Task<OwnerLockoutCheck> BeforeChangeAsync(CancellationToken cancellationToken = default) =>
        new(this, await staff.AnyActiveUnrestrictedAsync(cancellationToken));

    private async Task EnsureAnOwnerRemainsAsync(CancellationToken cancellationToken)
    {
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!await staff.AnyActiveUnrestrictedAsync(cancellationToken))
        {
            throw new LastOwnerException();
        }
    }

    /// <summary>What <see cref="BeforeChangeAsync"/> observed, to be settled once the change is made.</summary>
    public sealed class OwnerLockoutCheck
    {
        private readonly OwnerLockoutGuard _guard;
        private readonly bool _hadOwner;

        internal OwnerLockoutCheck(OwnerLockoutGuard guard, bool hadOwner) =>
            (_guard, _hadOwner) = (guard, hadOwner);

        /// <summary>
        /// Call as the last step of the transaction. Throws <c>409 last_owner</c> if the change
        /// removed the only account holding <c>*</c>. A clinic that had no such account to begin
        /// with is not made worse by the change, so it is let through.
        /// </summary>
        public Task EnsureAnOwnerRemainsAsync(CancellationToken cancellationToken = default) =>
            _hadOwner ? _guard.EnsureAnOwnerRemainsAsync(cancellationToken) : Task.CompletedTask;
    }
}

/// <summary>
/// <c>409 last_owner</c> — the change would leave no active account holding <c>*</c>. Not in the
/// API reference's error list.
/// </summary>
public sealed class LastOwnerException : AppException
{
    public LastOwnerException()
        : base("last_owner", "ต้องมีผู้ใช้ที่มีสิทธิ์ทั้งหมด (*) อย่างน้อยหนึ่งคนเสมอ", HttpStatus.Conflict)
    {
        MessageEn = "This change would leave the clinic with no active account holding every scope.";
    }
}
