using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.AssignTicket;

/// <summary>
/// <c>POST /staff/queue/{id}/assign</c> — "จ่ายหมอและห้องตรวจ · ย้ายคิวข้ามห้องได้".
/// </summary>
public sealed record AssignTicketCommand(
    string TicketId,
    string? StaffId = null,
    string? RoomId = null,
    uint? IfMatchVersion = null);

public sealed class AssignTicketValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketValidator() =>
        RuleFor(command => command)
            .Must(command => command.StaffId is { Length: > 0 } || command.RoomId is { Length: > 0 })
            .WithName("staff_id")
            .WithMessage("ต้องระบุหมอหรือห้องตรวจอย่างน้อยหนึ่งอย่าง");
}

/// <summary>
/// Puts a vet and a room on a ticket, or moves it to a different room.
/// <para>
/// The room is checked against the branch's own registry. Accepting any string would let a typo
/// send an owner to a door that does not exist, and the queue board is read by people in a waiting
/// room rather than by a developer who can tell that <c>room_22</c> was meant to be <c>room_2</c>.
/// </para>
/// <para>
/// The vet is checked the same way (<see cref="TicketVetGuard"/>): an active vet at the ticket's
/// branch, or <c>422</c> at <c>staff_id</c>. <c>docs/contract-extensions.md § 31</c>.
/// </para>
/// </summary>
public sealed class AssignTicketHandler(
    QueueAccess access,
    IBookingCatalog catalog,
    TicketVetGuard vets,
    IQueueEventStream events,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<QueueTicketDto> HandleAsync(
        AssignTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        QueueTicket? assigned = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ticket = await access.ForStaffAsync(command.TicketId, token);

            ConcurrencyGuard.Ensure(ticket, command.IfMatchVersion);

            if (command.RoomId is { Length: > 0 } roomId)
            {
                var branch = await catalog.FindBranchAsync(ticket.BranchId, token)
                    ?? throw new NotFoundException("branch", ticket.BranchId);

                if (!branch.HasExamRoom(roomId))
                {
                    throw new ValidationFailedException(
                        new Dictionary<string, string[]> { ["room_id"] = ["ไม่พบห้องตรวจนี้ในสาขา"] });
                }
            }

            // Only a change is checked: re-sending the vet already on the ticket (a retry, or a
            // room move that repeats the vet) does not fail because that vet has since left.
            if (command.StaffId is { Length: > 0 } staffId && staffId != ticket.StaffId)
            {
                await vets.EnsureVetAtBranchAsync(staffId, ticket.BranchId, token);
            }

            ticket.Assign(command.StaffId, command.RoomId);
            assigned = ticket;
        }, cancellationToken);

        events.Publish(new QueueEvent(
            QueueEventTypes.TicketUpdated, assigned!.BranchId, assigned.Id, assigned.CustomerId));

        return QueueTicketDto.From(assigned);
    }
}
