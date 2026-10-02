using System.Globalization;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Appointments;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Appointments.AddToWaitingList;

/// <summary>
/// "ยิง LINE อัตโนมัติเมื่อมีคนยกเลิก" — tells the people waiting that a slot they would take is free.
/// <para>
/// <b>Told, not booked.</b> The freed slot is not handed to anyone. Up to
/// <see cref="MaxToNotify"/> people, in the order they joined, are told it exists, and whoever books
/// it first through the ordinary booking path has it. Reserving it for the first person would need a
/// hold that expires and passes to the next, which is a job scheduler this system does not have;
/// telling a few at once and letting the booking path settle it cannot double-book anyone.
/// </para>
/// <para>
/// Runs after the cancellation has committed and never throws. A waiting-list message that could
/// not be sent is not a reason to say the cancellation failed.
/// </para>
/// </summary>
public sealed class WaitingListNotifier(
    IWaitingListRepository waitingList,
    IPetRepository pets,
    IBookingCatalog catalog,
    INotificationDispatcher notifications,
    IUnitOfWork unitOfWork,
    ClinicDays clinicDays,
    IClock clock,
    ILogger<WaitingListNotifier> logger)
{
    /// <summary>A few, so the slot is likely taken; not everyone, so the list is not spammed for one opening.</summary>
    public const int MaxToNotify = 3;

    public async Task SlotFreedAsync(Appointment freed, CancellationToken cancellationToken = default)
    {
        try
        {
            await NotifyAsync(freed, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Waiting list could not be notified for appointment {AppointmentId}", freed.Id);
        }
    }

    private async Task NotifyAsync(Appointment freed, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        // A slot in the past is not an opening.
        if (freed.StartsAt <= now)
        {
            return;
        }

        var freedOn = clinicDays.DateOf(freed.StartsAt);
        var told = new List<WaitingListEntry>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            told.Clear();

            var waiting = await waitingList.WaitingForAsync(freed.ServiceId, freedOn, token);

            foreach (var entry in waiting.Where(entry => entry.Fits(freed, freedOn)).Take(MaxToNotify))
            {
                entry.MarkNotified(now);
                told.Add(entry);
            }
        }, cancellationToken);

        if (told.Count == 0)
        {
            return;
        }

        var service = await catalog.FindServiceAsync(freed.ServiceId, cancellationToken);
        var local = TimeZoneInfo.ConvertTime(freed.StartsAt, clock.ClinicTimeZone);

        foreach (var entry in told)
        {
            var pet = await pets.FindByIdAsync(entry.PetId, cancellationToken);

            await notifications.SendAsync(
                new NotificationRequest(
                    entry.CustomerId,
                    NotificationTemplates.WaitingListSlotAvailable,
                    new Dictionary<string, string>
                    {
                        ["pet_name"] = pet?.Name ?? string.Empty,
                        ["service_name"] = service?.Name ?? string.Empty,
                        ["date"] = local.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH")),
                        ["time"] = local.ToString("HH:mm", CultureInfo.InvariantCulture),
                        ["waiting_list_id"] = entry.Id,
                    }),
                cancellationToken);
        }
    }
}
