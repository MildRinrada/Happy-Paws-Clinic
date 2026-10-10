using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.IssueWalkInTicket;

/// <summary><c>POST /staff/queue/tickets</c> — "ออกบัตรคิวให้ walk-in ที่ยังไม่มีนัด".</summary>
public sealed record IssueWalkInTicketCommand(
    string PetId,
    string ServiceId,
    string? BranchId = null,
    string? StaffId = null,
    string? Priority = null,
    string? PriorityReason = null);

public sealed class IssueWalkInTicketValidator : AbstractValidator<IssueWalkInTicketCommand>
{
    public IssueWalkInTicketValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();

        // A walk-in arriving as an emergency is the whole reason the counter needs this field, and
        // an unexplained jump up the queue is exactly what must not be possible.
        RuleFor(command => command.PriorityReason)
            .NotEmpty()
            .When(command => command.Priority is { Length: > 0 } and not "normal")
            .WithMessage("ต้องระบุเหตุผลเมื่อออกบัตรคิวแบบเร่งด่วน");
    }
}

/// <summary>
/// A number for someone who walked in without an appointment.
/// <para>
/// No appointment is created. The contract keeps the two separate — a walk-in that also wants a
/// record on the calendar goes through <c>POST /staff/appointments</c> with <c>walk_in</c> and is
/// checked in — and inventing an appointment here would put a phantom booking on a vet's day.
/// </para>
/// </summary>
public sealed class IssueWalkInTicketHandler(
    IQueueTicketRepository tickets,
    IQueueNumberGenerator numbers,
    IQueueEventStream events,
    IPetRepository pets,
    IBookingCatalog catalog,
    IClinicSettingsStore settings,
    BranchGuard branchGuard,
    TicketVetGuard vets,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<QueueTicketDto> HandleAsync(
        IssueWalkInTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var serviceDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(clock.UtcNow, clock.ClinicTimeZone).DateTime);

        QueueTicket? ticket = null;

        // Loaded inside the transaction, because a retry resets the change tracker — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            if (pet.IsDeceased)
            {
                throw new ValidationFailedException(
                    new Dictionary<string, string[]> { ["pet_id"] = ["เรกคอร์ดของสัตว์เลี้ยงตัวนี้ถูกปิดแล้ว"] });
            }

            var branch = await ResolveBranchAsync(command.BranchId, token);
            branchGuard.EnsureCanAccess(branch);

            var service = await catalog.FindServiceAsync(command.ServiceId, token)
                ?? throw new NotFoundException("service", command.ServiceId);

            // The same check as assigning a vet later (docs/contract-extensions.md § 31).
            if (command.StaffId is { Length: > 0 } staffId)
            {
                await vets.EnsureVetAtBranchAsync(staffId, branch, token);
            }

            var queue = await settings.QueueAsync(token);
            var sequence = await numbers.NextAsync(branch, serviceDate, queue.ResetDaily, token);

            ticket = QueueTicket.IssueWalkIn(
                branch,
                pet.Id,
                pet.PrimaryCustomerId,
                service.Id,
                sequence,
                serviceDate,
                clock.UtcNow,
                command.StaffId,
                queue.Format(sequence));

            // Set rather than passed to the factory, so an urgent walk-in records who decided that
            // and why through exactly the same path as a priority raised later at the board.
            var priority = QueuePriorityParser.Parse(command.Priority);
            if (priority != QueuePriority.Normal)
            {
                ticket.SetPriority(priority, command.PriorityReason, currentUser.RequireStaffId(), clock.UtcNow);
            }

            tickets.Add(ticket);
        }, cancellationToken);

        // After commit: a board that has not been written yet is not a board worth announcing.
        events.Publish(new QueueEvent(
            QueueEventTypes.TicketUpdated, ticket!.BranchId, ticket.Id, ticket.CustomerId));

        return QueueTicketDto.From(ticket);
    }

    private async Task<string> ResolveBranchAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 })
        {
            var branch = await catalog.FindBranchAsync(branchId, cancellationToken)
                ?? throw new NotFoundException("branch", branchId);

            return branch.Id;
        }

        var sole = await catalog.SoleActiveBranchAsync(cancellationToken);

        return sole?.Id ?? throw new ValidationFailedException(
            new Dictionary<string, string[]> { ["branch_id"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });
    }
}
