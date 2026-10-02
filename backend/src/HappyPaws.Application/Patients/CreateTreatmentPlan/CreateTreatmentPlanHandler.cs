using System.Globalization;
using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.CreateRecurringSeries;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.CreateTreatmentPlan;

/// <summary>
/// <c>POST /staff/pets/{id}/treatment-plans</c> — "แผนหลายครั้ง · ผูกกับนัดและการเตือน".
/// <para>
/// The sessions come from <c>Rrule</c>, or from <c>Sessions</c> and <c>IntervalDays</c>, or from
/// the template's course when <c>TemplateId</c> names one with sessions. <c>Book</c> (default true)
/// books every session as an appointment; false plans the dates without booking them.
/// </para>
/// </summary>
public sealed record CreateTreatmentPlanCommand(
    string PetId,
    string ServiceId,
    DateTimeOffset FirstStartsAt,
    string? Title = null,
    string? Rrule = null,
    int? Sessions = null,
    int? IntervalDays = null,
    string? TemplateId = null,
    string? ConsultationId = null,
    string? StaffId = null,
    string? BranchId = null,
    string? Notes = null,
    bool Book = true,
    string? Type = null);

public sealed class CreateTreatmentPlanValidator : AbstractValidator<CreateTreatmentPlanCommand>
{
    public CreateTreatmentPlanValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.FirstStartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาของครั้งแรก");
        RuleFor(command => command.Title).MaximumLength(200);
        RuleFor(command => command.Rrule).MaximumLength(300);
        RuleFor(command => command.Notes).MaximumLength(2_000);
        RuleFor(command => command.Sessions).InclusiveBetween(2, TreatmentPlan.MaxSessions);
        RuleFor(command => command.IntervalDays).InclusiveBetween(1, 365);

        RuleFor(command => command.IntervalDays)
            .NotNull().WithMessage("ส่ง sessions คู่กับ interval_days")
            .When(command => command.Sessions is not null);
    }
}

/// <summary>
/// Plans a course of treatment and, normally, books it.
/// <para>
/// <b>"ผูกกับนัด"</b> is literal: each session is booked as an appointment through the same path as
/// <c>POST /staff/appointments/recurring</c>, all or nothing, with one vet for the course, and each
/// session records the appointment that holds it. <b>"การเตือน"</b> is carried by those
/// appointments: the reminders the Automation module sends are about appointments, so a plan whose
/// sessions are booked is a plan that will be reminded. That module is not built yet; see
/// <c>docs/assumptions.md § 38</c>.
/// </para>
/// </summary>
public sealed class CreateTreatmentPlanHandler(
    IPetRepository pets,
    ITreatmentPlanRepository plans,
    ITreatmentTemplateRepository templates,
    IConsultationRepository consultations,
    SeriesBooker booker,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<TreatmentPlanDto> HandleAsync(
        CreateTreatmentPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();

        var pet = await pets.FindByIdAsync(command.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", command.PetId);

        if (pet.IsDeceased)
        {
            throw new StateConflictException(nameof(Pet), "deceased", "create_treatment_plan");
        }

        var template = command.TemplateId is { Length: > 0 } templateId
            ? await templates.FindActiveAsync(templateId, cancellationToken) ?? throw new NotFoundException("treatment_template", templateId)
            : null;

        var title = command.Title ?? template?.Name
            ?? throw new ValidationFailedException(new Dictionary<string, string[]> { ["title"] = ["ต้องระบุชื่อแผน หรือเลือกเทมเพลต"] });

        var starts = CreateRecurringSeriesHandler.Expand(command.FirstStartsAt, RuleFor(command, template), clock.ClinicTimeZone);
        var type = await settings.RequireTypeAsync(command.Type, cancellationToken);

        TreatmentPlan? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (command.ConsultationId is { Length: > 0 } consultationId)
            {
                var consultation = await consultations.FindByIdAsync(consultationId, token);
                if (consultation is null || consultation.PetId != pet.Id)
                {
                    throw new NotFoundException("consultation", consultationId);
                }
            }

            var plan = TreatmentPlan.Create(
                pet.Id, title, command.ServiceId, starts, staffId, command.ConsultationId, template?.Id, command.Notes ?? template?.Plan);

            if (command.Book)
            {
                var (seriesId, visits) = await booker.BookAsync(
                    new SeriesRequest(pet, command.ServiceId, starts, type, staffId, command.StaffId, command.BranchId, title),
                    token);

                plan.LinkAppointments(seriesId, [.. visits.Select(visit => (visit.Id, visit.StartsAt))]);
            }

            plans.Add(plan);

            audit.Record(new AuditEvent(
                AuditActions.TreatmentPlanCreated,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["treatment_plan_id"] = plan.Id,
                    ["sessions"] = plan.Sessions.Count,
                    ["booked"] = command.Book,
                }));

            created = plan;
        }, cancellationToken);

        return TreatmentPlanDto.From(created!);
    }

    /// <summary>The repeat rule, from whichever of the three ways the caller described the course.</summary>
    private static string RuleFor(CreateTreatmentPlanCommand command, TreatmentTemplate? template)
    {
        if (command.Rrule is { Length: > 0 } rule)
        {
            return rule;
        }

        if (command.Sessions is { } sessions && command.IntervalDays is { } interval)
        {
            return Daily(sessions, interval);
        }

        if (template is { Sessions: { } templateSessions, SessionIntervalDays: { } templateInterval })
        {
            return Daily(templateSessions, templateInterval);
        }

        throw new ValidationFailedException(new Dictionary<string, string[]>
        {
            ["rrule"] = ["ส่ง rrule หรือ sessions คู่กับ interval_days หรือเลือกเทมเพลตที่เป็นคอร์ส"],
        });
    }

    private static string Daily(int sessions, int intervalDays) =>
        string.Create(CultureInfo.InvariantCulture, $"FREQ=DAILY;INTERVAL={intervalDays};COUNT={sessions}");
}
