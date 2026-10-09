using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Patients.MarkPetDeceased;

/// <summary>
/// <c>POST /staff/pets/{id}/deceased</c> —
/// "ปิดเรกคอร์ดอย่างสุภาพ · หยุดแจ้งเตือนทุกชนิด".
/// </summary>
public sealed record MarkPetDeceasedCommand(string PetId, DateTimeOffset? OccurredAt = null, string? Note = null);

/// <summary>What closing a patient record did, so the counter can see it and say so.</summary>
public sealed record PetDeceasedResult(
    string PetId,
    DateTimeOffset DeceasedAt,
    IReadOnlyList<string> CancelledAppointmentIds);

/// <summary>
/// Closes a patient record and clears the calendar behind it.
/// <para>
/// <b>The cancellation rule is this project's, not the contract's.</b> The reference says to stop
/// all notifications and says nothing about visits already booked, which leaves the worst possible
/// default in place: a reminder, or a phone call from the counter, asking a grieving owner to
/// confirm an appointment for a pet that has died. Both alternatives were considered and cancelling
/// won — leaving the bookings would produce exactly that call, and the slots are also real clinic
/// time that someone else needs. See <c>docs/contract-extensions.md § 6</c>.
/// </para>
/// <para>
/// Cancelling silently would be its own failure, so one message goes out afterwards — one, not one
/// per appointment. The rest of the record stays exactly where it is: history is never deleted, and
/// "ปิดเรกคอร์ดอย่างสุภาพ" is about stopping the clinic from reaching out, not about erasing a
/// patient.
/// </para>
/// </summary>
public sealed class MarkPetDeceasedHandler(
    IPetRepository pets,
    IAppointmentRepository appointments,
    INotificationDispatcher notifications,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<MarkPetDeceasedHandler> logger)
{
    public async Task<PetDeceasedResult> HandleAsync(
        MarkPetDeceasedCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("caller is not authenticated");
        var now = clock.UtcNow;
        var deceasedAt = command.OccurredAt ?? now;

        var cancelled = new List<string>();
        string? customerId = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // The list is rebuilt on every attempt: a retry re-runs from here with a clean change
            // tracker, so anything collected outside would be detached — see IUnitOfWork.
            cancelled.Clear();

            var pet = await pets.FindTrackedByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            pet.MarkDeceased(deceasedAt);
            customerId = pet.PrimaryCustomerId;

            var upcoming = await appointments.UpcomingForPetAsync(pet.Id, now, token);

            foreach (var appointment in upcoming)
            {
                appointment.Cancel(now, userId, "สัตว์เลี้ยงเสียชีวิต");
                cancelled.Add(appointment.Id);
                webhooks.Enqueue(WebhookEventTypes.AppointmentCancelled, WebhookPayloads.Appointment(appointment));

                audit.Record(new AuditEvent(
                    AuditActions.AppointmentCancelled,
                    AuditResources.Appointment,
                    appointment.Id,
                    pet.Id,
                    appointment.BranchId,
                    new Dictionary<string, object?> { ["reason"] = "pet_deceased" }));
            }

            audit.Record(new AuditEvent(
                AuditActions.PetMarkedDeceased,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["deceased_at"] = deceasedAt,
                    ["cancelled_appointments"] = cancelled.Count,
                    ["note"] = command.Note,
                }));
        }, cancellationToken);

        if (cancelled.Count > 0 && customerId is { Length: > 0 })
        {
            await NotifyOwnerAsync(customerId, command.PetId, cancelled.Count, cancellationToken);
        }

        return new PetDeceasedResult(command.PetId, deceasedAt, cancelled);
    }

    private async Task NotifyOwnerAsync(
        string customerId,
        string petId,
        int cancelledCount,
        CancellationToken cancellationToken)
    {
        try
        {
            await notifications.SendAsync(
                new NotificationRequest(
                    customerId,
                    NotificationTemplates.PetDeceasedAppointmentsCancelled,
                    new Dictionary<string, string>
                    {
                        ["pet_id"] = petId,
                        ["cancelled_count"] = cancelledCount.ToString(),
                    }),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Could not tell customer {CustomerId} that {Count} appointments for pet {PetId} were cancelled",
                customerId,
                cancelledCount,
                petId);
        }
    }
}
