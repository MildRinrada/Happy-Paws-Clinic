using FluentValidation;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Appointments.ListWaitingList;

/// <summary>
/// <c>GET /staff/waiting-list?branch=&amp;service_id=&amp;status=&amp;limit=&amp;cursor=</c>. Not in the
/// API reference: see <c>docs/contract-extensions.md § 23</c>.
/// <para>
/// <c>status</c> is <c>waiting</c> (the last day has not passed), <c>lapsed</c>, or absent for both.
/// </para>
/// </summary>
public sealed record ListWaitingListQuery(
    string? BranchId = null,
    string? ServiceId = null,
    string? Status = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

public sealed class ListWaitingListValidator : AbstractValidator<ListWaitingListQuery>
{
    public ListWaitingListValidator()
    {
        // Ignoring an unknown status would answer with the whole list and look like a filtered one.
        RuleFor(query => query.Status)
            .Must(status => status is WaitingListItemDto.Waiting or WaitingListItemDto.Lapsed)
            .WithMessage($"status ต้องเป็น {WaitingListItemDto.Waiting} หรือ {WaitingListItemDto.Lapsed}")
            .When(query => !string.IsNullOrEmpty(query.Status));

        RuleFor(query => query.BranchId).MaximumLength(40);
        RuleFor(query => query.ServiceId).MaximumLength(40);
    }
}

/// <summary>
/// The waiting list, oldest first: the order in which the people waiting are told about an opening.
/// <para>
/// Branch-bound staff see the entries that name one of their branches and the entries that name
/// none, because an opening at their branch reaches both. Asking for another branch is <c>403</c>.
/// The restriction is a filter in the query, not a check on the result, so paging cannot pass it.
/// </para>
/// </summary>
public sealed class ListWaitingListHandler(
    IWaitingListRepository waitingList,
    BranchGuard branchGuard,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<CursorPage<WaitingListItemDto>> HandleAsync(
        ListWaitingListQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        if (query.BranchId is { Length: > 0 } requested)
        {
            branchGuard.EnsureCanAccess(requested);
        }

        var today = clinicDays.Today;

        var filter = new WaitingListFilter(
            today,
            query.BranchId,
            query.ServiceId,
            query.Status switch
            {
                WaitingListItemDto.Waiting => false,
                WaitingListItemDto.Lapsed => true,
                _ => null,
            },
            branchGuard.VisibleBranchIds);

        var page = await waitingList.ListAsync(filter, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<WaitingListItemDto>(
            [.. page.Data.Select(row => WaitingListItemDto.From(row.Entry, row.PetName, row.ServiceName, today))],
            page.NextCursor);
    }
}
