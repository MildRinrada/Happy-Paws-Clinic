using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.CreateStaffAppointment;

/// <summary><c>POST /staff/appointments</c> — "จองแทนลูกค้าและรับ walk-in · ข้าม lead time ได้".</summary>
public sealed record CreateStaffAppointmentCommand(
    string PetId,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null,
    string? Reason = null,
    bool WalkIn = false,
    string? Type = null);

public sealed class CreateStaffAppointmentValidator : AbstractValidator<CreateStaffAppointmentCommand>
{
    public CreateStaffAppointmentValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาเริ่ม");
        RuleFor(command => command.Reason).MaximumLength(1000);
    }
}

/// <summary>
/// The counter booking on a customer's behalf, including the pet that just walked in.
/// <para>
/// Two things separate this from the customer path. Staff bypass the lead time, because the animal
/// is already in the waiting room. And staff may book services that are closed to online booking,
/// because "closed to online booking" means "we want to talk to you first", not "we do not do
/// this" — the conversation has just happened. The same goes for appointment types kept off the
/// booking page: the counter books an emergency, the website does not.
/// </para>
/// <para>
/// Ownership is not asked about here: a staff member holding <c>appointment:write</c> books for
/// whoever is standing at the counter. The customer on the appointment is the pet's registered
/// primary owner, which is who the visit gets billed to.
/// </para>
/// </summary>
public sealed class CreateStaffAppointmentHandler(
    BookingSlotResolver resolver,
    IAppointmentRepository appointments,
    IPetRepository pets,
    IClinicSettingsStore settings,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<AppointmentDto> HandleAsync(
        CreateStaffAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();

        var pet = await pets.FindByIdAsync(command.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", command.PetId);

        if (pet.IsDeceased)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["pet_id"] = ["เรกคอร์ดของสัตว์เลี้ยงตัวนี้ถูกปิดแล้ว"] });
        }

        var type = await settings.RequireTypeAsync(command.Type, cancellationToken);

        Appointment? appointment = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    command.ServiceId,
                    command.StaffId,
                    command.BranchId,
                    command.StartsAt,
                    BypassLeadTime: true,
                    RequireOnlineBookable: false,
                    Species: pet.Species,
                    DurationMinutes: type.DurationMinutes),
                token);

            // Checked after the branch is resolved, so omitting branch_id in a multi-branch clinic
            // cannot land a booking at a site the caller does not work at.
            branchGuard.EnsureCanAccess(slot.Branch.Id);

            appointment = Appointment.Book(
                slot.Branch.Id,
                pet.PrimaryCustomerId,
                pet.Id,
                slot.Service.Id,
                slot.StaffId,
                slot.StartsAt,
                slot.EndsAt,
                command.Reason,
                isWalkIn: command.WalkIn,
                bookedByStaffId: staffId,
                typeCode: type.Code);

            appointments.Add(appointment);
        }, cancellationToken);

        return AppointmentDto.From(appointment!);
    }
}
