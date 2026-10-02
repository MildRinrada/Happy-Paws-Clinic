using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Appointments.ListAppointments;

/// <summary>
/// <c>GET /staff/appointments?date=&amp;status=&amp;vet_id=&amp;branch=&amp;q=</c> —
/// "ตารางวันนี้ของทั้งคลินิกหรือรายหมอ".
/// </summary>
public sealed record ListAppointmentsQuery(
    DateOnly? Date = null,
    string? Status = null,
    string? StaffId = null,
    string? BranchId = null,
    string? Query = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The clinic's board for a day.
/// <para>
/// The branch restriction is resolved from the caller's token and handed to the repository as a
/// filter, not applied to the result. A caller whose token names two branches must not be able to
/// page past them by asking for a third, and filtering after the query would still have read the
/// rows.
/// </para>
/// </summary>
public sealed class ListAppointmentsHandler(
    IAppointmentRepository appointments,
    BranchGuard branchGuard)
{
    public async Task<CursorPage<AppointmentDto>> HandleAsync(
        ListAppointmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.BranchId is { Length: > 0 } requested)
        {
            branchGuard.EnsureCanAccess(requested);
        }

        var status = AppointmentDto.ParseWireStatus(query.Status);
        if (query.Status is { Length: > 0 } && status is null)
        {
            // Silently ignoring an unrecognised filter would answer with the whole board and look
            // like a successful query for a status that does not exist.
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["status"] = ["สถานะนัดไม่ถูกต้อง"] });
        }

        var filter = new AppointmentListFilter(
            query.Date,
            status,
            query.StaffId,
            query.BranchId,
            query.Query,
            branchGuard.VisibleBranchIds);

        var page = await appointments.ListForStaffAsync(
            filter,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<AppointmentDto>([.. page.Data.Select(AppointmentDto.From)], page.NextCursor);
    }
}
