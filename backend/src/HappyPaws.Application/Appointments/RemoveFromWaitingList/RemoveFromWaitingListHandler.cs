using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Appointments.RemoveFromWaitingList;

/// <summary>
/// <c>DELETE /staff/waiting-list/{id}</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 23</c>.
/// </summary>
/// <param name="EntryId">The entry.</param>
public sealed record RemoveFromWaitingListCommand(string EntryId);

public sealed class RemoveFromWaitingListValidator : AbstractValidator<RemoveFromWaitingListCommand>
{
    public RemoveFromWaitingListValidator()
    {
        RuleFor(command => command.EntryId).NotEmpty().MaximumLength(40);
    }
}

/// <summary>
/// Takes a pet off the waiting list: the owner booked elsewhere, changed their mind, or was added
/// by mistake.
/// <para>
/// <b>The row is deleted.</b> An entry has no status to move to (a withdrawn flag would need a
/// migration), nothing else points at it, and the messages it already caused are in the delivery
/// log on their own. What is worth keeping is that someone removed it, which the audit row holds
/// together with what the entry was. Removing it again answers <c>404</c>.
/// </para>
/// <para>
/// An entry that names a branch the caller is not assigned to is <c>403</c>. One that names no
/// branch belongs to every branch, as it does in the list, so any branch's counter may remove it.
/// </para>
/// </summary>
public sealed class RemoveFromWaitingListHandler(
    IWaitingListRepository waitingList,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task HandleAsync(RemoveFromWaitingListCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        _ = currentUser.RequireStaffId();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var entry = await waitingList.FindByIdAsync(command.EntryId, token)
                ?? throw new NotFoundException("waiting_list_entry", command.EntryId);

            if (entry.BranchId is { } branchId)
            {
                branchGuard.EnsureCanAccess(branchId);
            }

            waitingList.Remove(entry);

            audit.Record(new AuditEvent(
                AuditActions.WaitingListEntryRemoved,
                AuditResources.WaitingList,
                entry.Id,
                PetId: entry.PetId,
                BranchId: entry.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["customer_id"] = entry.CustomerId,
                    ["service_id"] = entry.ServiceId,
                    ["staff_id"] = entry.StaffId,
                    ["earliest_on"] = entry.EarliestOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    ["latest_on"] = entry.LatestOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    ["notified_count"] = entry.NotifiedCount,
                }));
        }, cancellationToken);
    }
}
