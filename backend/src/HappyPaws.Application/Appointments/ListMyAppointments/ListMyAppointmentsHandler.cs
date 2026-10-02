using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Appointments.ListMyAppointments;

/// <summary>
/// <c>GET /portal/appointments</c> — "นัดที่จะถึงและประวัติย้อนหลัง".
/// <para>
/// <c>Upcoming</c> true returns future visits soonest-first; false returns the history
/// newest-first. The portal shows these as two lists, and one flag is enough to serve both.
/// </para>
/// </summary>
public sealed record ListMyAppointmentsQuery(
    bool Upcoming = true,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The customer's own appointments.
/// <para>
/// Scoped by <c>customer_id</c> from the token at the database, so there is no filter for a caller
/// to tamper with — the query cannot express "someone else's appointments" at all.
/// </para>
/// </summary>
public sealed class ListMyAppointmentsHandler(
    IAppointmentRepository appointments,
    ICurrentUser currentUser)
{
    public async Task<CursorPage<AppointmentDto>> HandleAsync(
        ListMyAppointmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await appointments.ListForCustomerAsync(
            currentUser.RequireCustomerId(),
            query.Upcoming,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<AppointmentDto>([.. page.Data.Select(AppointmentDto.From)], page.NextCursor);
    }
}
