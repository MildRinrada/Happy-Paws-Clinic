using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.MergeCustomers;

/// <summary>
/// <c>POST /staff/customers/{id}/merge</c> — "รวมเรกคอร์ดซ้ำ · ย้ายสัตว์และประวัติทั้งหมด".
/// <para>
/// <b>The path id is the record that survives</b>, and the body names the duplicate it absorbs.
/// The contract does not say which way round it goes. This reading was chosen because it is the one
/// that fails safe when someone gets it backwards: you post to the record you have open and have
/// decided to keep, and the id you type is the one you are throwing away.
/// </para>
/// </summary>
public sealed record MergeCustomersCommand(
    string SurvivingCustomerId,
    string DuplicateCustomerId,
    uint? IfMatchVersion = null);

public sealed class MergeCustomersValidator : AbstractValidator<MergeCustomersCommand>
{
    public MergeCustomersValidator()
    {
        RuleFor(command => command.DuplicateCustomerId).NotEmpty();

        RuleFor(command => command)
            .Must(command => command.SurvivingCustomerId != command.DuplicateCustomerId)
            .WithMessage("รวมเรกคอร์ดเข้ากับตัวเองไม่ได้");
    }
}

/// <summary>
/// Folds a duplicate customer record into the one being kept.
/// <para>
/// This is the most destructive operation in the API. It moves patients, appointments, clinical
/// records, invoices and consent decisions between records, and nothing here undoes it. Three
/// things follow from that, and all three are deliberate.
/// </para>
/// <para>
/// <b>The duplicate is kept as a tombstone.</b> It is marked merged and inactive, never deleted, so
/// an id on an old receipt still resolves — and resolves to a record that says where it went. Every
/// audit row written before today still names an id that exists.
/// </para>
/// <para>
/// <b>Two portal accounts refuse the merge.</b> If both records carry a login, they are two real
/// credentials for one person; choosing which to discard is a decision with a security consequence
/// and it is not one an API should take silently. Everything else about the merge is arithmetic;
/// this is not. Answered as <c>409 state_conflict</c>.
/// </para>
/// <para>
/// <b>The row moves are set-based.</b> Nine tables are reassigned with one <c>UPDATE</c> each rather
/// than by loading entities — a customer of ten years has hundreds of rows, and loading them to
/// change one column would be slow and would gain nothing, because none of those rows has an
/// invariant that depends on which customer they point at. The counts come back and go into both
/// the response and the audit entry, so what moved is answerable afterwards.
/// </para>
/// </summary>
public sealed class MergeCustomersHandler(
    ICustomerRepository customers,
    IPetOwnershipRepository ownerships,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<MergeResultDto> HandleAsync(
        MergeCustomersCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Customer? surviving = null;
        Customer? duplicate = null;
        IReadOnlyDictionary<string, int> moved = new Dictionary<string, int>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            surviving = await customers.FindTrackedByIdAsync(command.SurvivingCustomerId, token)
                ?? throw new NotFoundException("customer", command.SurvivingCustomerId);

            duplicate = await customers.FindTrackedByIdAsync(command.DuplicateCustomerId, token)
                ?? throw new NotFoundException("customer", command.DuplicateCustomerId);

            // If-Match on the survivor only. The duplicate is about to stop being a record
            // anyone edits, so a version check on it would guard nothing; the survivor is the one
            // a second operator might be changing underneath this.
            ConcurrencyGuard.Ensure(surviving, command.IfMatchVersion);

            EnsureMergeable(surviving, duplicate);

            // Carer links first, and separately from the bulk reassign: a link the survivor already
            // holds would violate the unique index the moment a blind UPDATE ran.
            var (folded, transferred) = await FoldCarerLinksAsync(duplicate.Id, surviving.Id, token);

            surviving.AdoptAccountFrom(duplicate);
            duplicate.MergeInto(surviving.Id);

            var counts = await customers.ReassignAsync(duplicate.Id, surviving.Id, token);

            moved = new Dictionary<string, int>(counts)
            {
                ["carer_links_moved"] = transferred,
                ["carer_links_folded"] = folded,
            };

            audit.Record(new AuditEvent(
                AuditActions.CustomerMerged,
                AuditResources.Customer,
                surviving.Id,
                Details: new Dictionary<string, object?>
                {
                    ["merged_customer_id"] = duplicate.Id,
                    ["merged_display_name"] = duplicate.DisplayName,
                    ["merged_phone"] = duplicate.Phone,
                    ["moved"] = moved,
                }));
        }, cancellationToken);

        return new MergeResultDto(
            CustomerDto.From(surviving!), CustomerDto.From(duplicate!), moved);
    }

    private static void EnsureMergeable(Customer surviving, Customer duplicate)
    {
        if (duplicate.IsMerged)
        {
            throw new StateConflictException(nameof(Customer), "merged", "merge");
        }

        if (surviving.IsMerged)
        {
            // Merging into a tombstone would put live patients behind a record nothing looks at.
            throw new StateConflictException(nameof(Customer), "merged", "merge_target");
        }

        if (surviving.UserId is not null && duplicate.UserId is not null)
        {
            throw new StateConflictException(nameof(Customer), "both_linked", "merge");
        }
    }

    /// <summary>
    /// Moves the duplicate's carer links onto the survivor, folding the ones it already holds.
    /// <para>
    /// Handled here with tracked entities rather than in the bulk <c>UPDATE</c>, because a blind
    /// reassignment would violate the unique index on
    /// <c>(pet_id, customer_id) WHERE revoked_at IS NULL</c> — correctly — whenever both records
    /// hold a live link to the same patient. Two links to one animal from what turns out to be one
    /// person is one link, and the survivor's takes the wider of the two flags: a merge tidies
    /// records, it never revokes access somebody had.
    /// </para>
    /// <para>
    /// The other case is the common one and it is easy to miss: a patient <b>only</b> the duplicate
    /// cares for. Its link has to move too, or the animal follows the merge in the invoice tables
    /// and quietly stops being visible to the person now responsible for it.
    /// </para>
    /// </summary>
    private async Task<(int Folded, int Moved)> FoldCarerLinksAsync(
        string duplicateId,
        string survivingId,
        CancellationToken cancellationToken)
    {
        var duplicateLinks = await ownerships.ForCustomerAsync(duplicateId, cancellationToken);
        var folded = 0;
        var moved = 0;

        foreach (var link in duplicateLinks.Where(link => link.IsActive))
        {
            var survivorLink = await ownerships.FindActiveAsync(link.PetId, survivingId, cancellationToken);

            if (survivorLink is null)
            {
                link.TransferTo(survivingId);
                moved++;

                continue;
            }

            survivorLink.Absorb(link);
            ownerships.Remove(link);
            folded++;
        }

        return (folded, moved);
    }
}
