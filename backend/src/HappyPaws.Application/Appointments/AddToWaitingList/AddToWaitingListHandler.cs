using FluentValidation;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Appointments.AddToWaitingList;

/// <summary>
/// <c>POST /staff/waiting-list</c> — "คิวรอคิวว่าง · ยิง LINE อัตโนมัติเมื่อมีคนยกเลิก". With no
/// dates, the window is today to <see cref="DefaultWindowDays"/> days ahead.
/// </summary>
public sealed record AddToWaitingListCommand(
    string PetId,
    string ServiceId,
    DateOnly? EarliestOn = null,
    DateOnly? LatestOn = null,
    string? BranchId = null,
    string? StaffId = null,
    string? Note = null)
{
    public const int DefaultWindowDays = 14;
}

public sealed class AddToWaitingListValidator : AbstractValidator<AddToWaitingListCommand>
{
    public AddToWaitingListValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.Note).MaximumLength(500);
    }
}

/// <summary>
/// Puts a pet on the waiting list for a service.
/// <para>
/// The customer is the pet's primary owner, the one the clinic would ring; the message goes to
/// them. One pet waits once per service: a second request while the first is still open is
/// refused with <c>409 already_waiting</c> rather than doubling the messages they get.
/// </para>
/// </summary>
public sealed class AddToWaitingListHandler(
    IWaitingListRepository waitingList,
    IPetRepository pets,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<WaitingListEntryDto> HandleAsync(
        AddToWaitingListCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var today = clinicDays.Today;

        var pet = await pets.FindByIdAsync(command.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", command.PetId);

        if (pet.IsDeceased)
        {
            throw new StateConflictException("Pet", "deceased", "add_to_waiting_list");
        }

        var service = await catalog.FindServiceAsync(command.ServiceId, cancellationToken);
        if (service is not { IsActive: true })
        {
            throw new NotFoundException("service", command.ServiceId);
        }

        if (command.BranchId is { Length: > 0 } branchId)
        {
            _ = await catalog.FindBranchAsync(branchId, cancellationToken) ?? throw new NotFoundException("branch", branchId);
            branchGuard.EnsureCanAccess(branchId);
        }

        WaitingListEntry? added = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await waitingList.IsWaitingAsync(pet.Id, service.Id, today, token))
            {
                throw new ConflictException("already_waiting", "สัตว์เลี้ยงตัวนี้อยู่ในคิวรอของบริการนี้แล้ว");
            }

            var earliest = command.EarliestOn ?? today;

            added = WaitingListEntry.Add(
                pet.PrimaryCustomerId,
                pet.Id,
                service.Id,
                earliest,
                command.LatestOn ?? earliest.AddDays(AddToWaitingListCommand.DefaultWindowDays),
                today,
                staffId,
                command.BranchId,
                command.StaffId,
                command.Note);

            waitingList.Add(added);
        }, cancellationToken);

        return WaitingListEntryDto.From(added!);
    }
}
