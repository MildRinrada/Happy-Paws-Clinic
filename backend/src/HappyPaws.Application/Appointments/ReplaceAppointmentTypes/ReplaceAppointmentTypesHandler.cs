using FluentValidation;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Appointments.ReplaceAppointmentTypes;

/// <summary>One type as sent. <c>QueuePriority</c> is <c>normal</c>, <c>urgent</c> or <c>emergency</c>.</summary>
public sealed record AppointmentTypeInput(
    string Code,
    string Name,
    int? DurationMinutes = null,
    bool OnlineBooking = true,
    int? LeadTimeMinutes = null,
    string? QueuePriority = null);

/// <summary><c>PUT /staff/appointment-types</c> — "นัดปกติ ฉุกเฉิน ติดตามผล ผ่าตัด · กติกาต่างกัน".</summary>
public sealed record ReplaceAppointmentTypesCommand(IReadOnlyList<AppointmentTypeInput> Types, uint? IfMatchVersion = null);

public sealed class ReplaceAppointmentTypesValidator : AbstractValidator<ReplaceAppointmentTypesCommand>
{
    public ReplaceAppointmentTypesValidator()
    {
        RuleFor(command => command.Types).NotEmpty();
        RuleForEach(command => command.Types).ChildRules(type =>
        {
            type.RuleFor(item => item.Code).NotEmpty().MaximumLength(30);
            type.RuleFor(item => item.Name).NotEmpty().MaximumLength(60);
            type.RuleFor(item => item.QueuePriority)
                .Must(value => WireEnum.TryParse<QueuePriority>(value, out _))
                .WithMessage($"queue_priority ต้องเป็น {WireEnum.Names<QueuePriority>()}")
                .When(item => item.QueuePriority is not null);
        });
    }
}

/// <summary>
/// Replaces the clinic's appointment types as a set.
/// <para>
/// Appointments already booked keep the code they were made with. A type that is removed stops
/// being bookable; the visits that carry it are not touched, and check-in treats a code it no
/// longer knows as an ordinary visit.
/// </para>
/// </summary>
public sealed class ReplaceAppointmentTypesHandler(
    IClinicSettingsStore settings,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<AppointmentTypesDto> HandleAsync(
        ReplaceAppointmentTypesCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var types = command.Types
            .Select(input => new AppointmentType(
                input.Code,
                input.Name,
                input.DurationMinutes,
                input.OnlineBooking,
                input.LeadTimeMinutes,
                WireEnum.TryParse<QueuePriority>(input.QueuePriority, out var priority) ? priority : QueuePriority.Normal))
            .ToList();

        AppointmentTypeCatalog? saved = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var catalog = await settings.AppointmentTypesForUpdateAsync(token);
            ConcurrencyGuard.Ensure(catalog, command.IfMatchVersion);

            catalog.Replace(types);
            saved = catalog;
        }, cancellationToken);

        return AppointmentTypesDto.From(saved!);
    }
}
