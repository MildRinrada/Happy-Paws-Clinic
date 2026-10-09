using FluentValidation;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Appointments.CreateRecurringSeries;

/// <summary>
/// <c>POST /staff/appointments/recurring</c> — "คอร์สรักษาต่อเนื่อง เช่น เคมีบำบัดทุก 3 สัปดาห์".
/// <c>Rrule</c> uses the same subset as shifts: <c>FREQ=WEEKLY;INTERVAL=3;COUNT=6</c> is the
/// contract's own example.
/// </summary>
public sealed record CreateRecurringSeriesCommand(
    string PetId,
    string ServiceId,
    DateTimeOffset FirstStartsAt,
    string Rrule,
    string? StaffId = null,
    string? BranchId = null,
    string? Reason = null,
    string? Type = null);

public sealed class CreateRecurringSeriesValidator : AbstractValidator<CreateRecurringSeriesCommand>
{
    public CreateRecurringSeriesValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.FirstStartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาของครั้งแรก");
        RuleFor(command => command.Rrule).NotEmpty().MaximumLength(300);
        RuleFor(command => command.Reason).MaximumLength(1_000);
    }
}

/// <summary>
/// Books a course of visits from a repeat rule.
/// <para>
/// The repeat rule is expanded in clinic time, so "every three weeks at 10:00" is 10:00 each time.
/// The whole course is booked or none of it is; see <see cref="SeriesBooker"/>. The visits share a
/// <c>series_id</c>, which is how the calendar shows them as one course.
/// </para>
/// </summary>
public sealed class CreateRecurringSeriesHandler(
    SeriesBooker booker,
    IPetRepository pets,
    IClinicSettingsStore settings,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<RecurringSeriesDto> HandleAsync(
        CreateRecurringSeriesCommand command,
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
        var starts = Expand(command.FirstStartsAt, command.Rrule, clock.ClinicTimeZone);

        var seriesId = string.Empty;
        IReadOnlyList<Appointment> visits = [];

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            (seriesId, visits) = await booker.BookAsync(
                new SeriesRequest(pet, command.ServiceId, starts, type, staffId, command.StaffId, command.BranchId, command.Reason),
                token);

            foreach (var visit in visits)
            {
                webhooks.Enqueue(WebhookEventTypes.AppointmentCreated, WebhookPayloads.Appointment(visit));
            }
        }, cancellationToken);

        return new RecurringSeriesDto(seriesId, [.. visits.Select(AppointmentDto.From)]);
    }

    /// <summary>The visit times, or <c>422</c> on <c>rrule</c> for a rule that cannot be read or is too long.</summary>
    public static IReadOnlyList<DateTimeOffset> Expand(DateTimeOffset first, string rule, TimeZoneInfo clinicTimeZone)
    {
        IReadOnlyList<DateTimeOffset> starts;

        try
        {
            starts = ShiftRecurrence.Expand(first, rule, clinicTimeZone);
        }
        catch (DomainException invalid)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["rrule"] = [invalid.Message] });
        }

        if (starts.Count < 2 || starts.Count > SeriesBooker.MaxVisits)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["rrule"] = [$"คอร์สต้องมี 2 ถึง {SeriesBooker.MaxVisits} ครั้ง กฎนี้ให้ {starts.Count} ครั้ง"],
            });
        }

        return starts;
    }
}
