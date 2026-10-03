using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.ListMyNotifications;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.ListVaccinations;
using HappyPaws.Application.Queueing;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Patients.Portal;

/// <summary>
/// A ticket on today's board, as its owner follows it. <c>PetId</c> and <c>AppointmentId</c> say
/// whose ticket it is, so an owner with two pets in on one day sees each number against the right
/// pet. <c>AppointmentId</c> is null for a walk-in.
/// </summary>
public sealed record PortalQueueDto(string TicketId, string Number, string Status, int Ahead, string PetId, string? AppointmentId);

/// <summary>A vaccine that is due or overdue for one of the owner's pets.</summary>
public sealed record PortalVaccinationDueDto(string PetId, string PetName, string VaccineName, DateOnly NextDueOn, string Status);

/// <summary>
/// <c>GET /portal/home</c> — "สรุปหน้าแรก: สัตว์ นัดถัดไป คิว แจ้งเตือน". The four the contract names,
/// plus vaccines falling due, which is the reminder an owner most needs on that page.
/// </summary>
public sealed record PortalHomeDto(
    string CustomerId,
    string DisplayName,
    IReadOnlyList<MyPetDto> Pets,
    AppointmentDto? NextAppointment,
    IReadOnlyList<PortalQueueDto> Queue,
    int UnreadNotifications,
    IReadOnlyList<InboxItemDto> LatestNotifications,
    IReadOnlyList<PortalVaccinationDueDto> VaccinationsDue);

/// <summary>
/// The portal's first page, in one request.
/// <para>
/// One round trip on purpose: this is what a phone loads when the owner opens the clinic's page,
/// and five requests over a mobile connection to draw one screen is a slow screen. Each part reads
/// what its own endpoint reads, so the summary cannot disagree with the page it links to.
/// </para>
/// </summary>
public sealed class GetPortalHomeHandler(
    ICustomerRepository customers,
    IPetRepository pets,
    MyPetReader petReader,
    IAppointmentRepository appointments,
    IQueueTicketRepository tickets,
    IInboxRepository inbox,
    IVaccinationRepository vaccinations,
    ClinicDays clinicDays,
    ICurrentUser currentUser)
{
    public async Task<PortalHomeDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        var customer = await customers.FindByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        var mine = await pets.ForCustomerAsync(customerId, cancellationToken);
        var petViews = new List<MyPetDto>(mine.Count);
        var due = new List<PortalVaccinationDueDto>();
        var today = clinicDays.Today;

        foreach (var pet in mine)
        {
            petViews.Add(await petReader.ReadAsync(pet, customerId, cancellationToken));

            if (!pet.IsDeceased)
            {
                var doses = await vaccinations.ForPetAsync(pet.Id, cancellationToken);
                due.AddRange(ListVaccinationsHandler.DueFrom(doses, today)
                    .Where(item => item.Status != "upcoming")
                    .Select(item => new PortalVaccinationDueDto(pet.Id, pet.Name, item.VaccineName, item.NextDueOn, item.Status)));
            }
        }

        var upcoming = await appointments.ListForCustomerAsync(customerId, upcomingOnly: true, new CursorPageRequest(10), cancellationToken);
        var next = upcoming.Data.FirstOrDefault(appointment => appointment.OccupiesSlot);

        var queue = new List<PortalQueueDto>();
        foreach (var ticket in await tickets.LiveForCustomerAsync(customerId, today, cancellationToken))
        {
            queue.Add(new PortalQueueDto(
                ticket.Id,
                ticket.Number,
                Queueing.Dtos.QueueTicketDto.ToWireStatus(ticket.Status),
                await tickets.CountAheadOfAsync(ticket, cancellationToken),
                ticket.PetId,
                ticket.AppointmentId));
        }

        var latest = await inbox.ListAsync(customerId, unreadOnly: false, new CursorPageRequest(3), cancellationToken);

        return new PortalHomeDto(
            customer.Id,
            customer.DisplayName,
            petViews,
            next is null ? null : AppointmentDto.From(next),
            queue,
            await inbox.UnreadCountAsync(customerId, cancellationToken),
            [.. latest.Data.Select(InboxItemDto.From)],
            [.. due.OrderBy(item => item.NextDueOn)]);
    }
}
