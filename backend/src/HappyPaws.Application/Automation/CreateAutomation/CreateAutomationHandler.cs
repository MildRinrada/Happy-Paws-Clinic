using System.Globalization;
using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation.CreateAutomation;

/// <summary>
/// <c>POST /staff/automations</c> — "เช่น ครบกำหนดวัคซีน 14 วัน → ส่ง LINE".
/// </summary>
/// <param name="Name">What staff call it.</param>
/// <param name="Trigger">What it watches and when it sends.</param>
/// <param name="Condition">Which subjects count. Optional.</param>
/// <param name="Action">What it sends. Optional: the trigger's template on <c>auto</c>.</param>
/// <param name="Enabled">Switched on now. Off unless asked, so a rule can be simulated first.</param>
public sealed record CreateAutomationCommand(
    string? Name,
    AutomationTriggerInput? Trigger,
    AutomationConditionInput? Condition = null,
    AutomationActionInput? Action = null,
    bool? Enabled = null);

/// <param name="Type"><c>vaccine_due</c>, <c>appointment_upcoming</c> or <c>treatment_plan_session</c>.</param>
/// <param name="OffsetDays">Days before the subject's day (negative is after, for vaccines).</param>
/// <param name="OffsetHours">Hours before the subject's start.</param>
/// <param name="SendTime"><c>HH:mm</c> clinic time, with <paramref name="OffsetDays"/>; 09:00 when absent.</param>
public sealed record AutomationTriggerInput(string? Type, int? OffsetDays = null, int? OffsetHours = null, string? SendTime = null);

/// <param name="BranchId">One branch; the caller's only branch, or the whole clinic for an unrestricted caller, when absent.</param>
/// <param name="Species">Only this species.</param>
/// <param name="VaccineName">Only this vaccine (vaccine rules).</param>
/// <param name="ServiceId">Only this service (appointment and plan rules).</param>
/// <param name="UnconfirmedOnly">Only appointments not yet confirmed (appointment rules).</param>
public sealed record AutomationConditionInput(
    string? BranchId = null,
    string? Species = null,
    string? VaccineName = null,
    string? ServiceId = null,
    bool? UnconfirmedOnly = null);

/// <param name="Type"><c>send_notification</c>, the only action.</param>
/// <param name="Channel"><c>auto</c> (default), <c>line</c>, <c>sms</c> or <c>email</c>.</param>
/// <param name="Template">The trigger's own template key; absent picks it.</param>
public sealed record AutomationActionInput(string? Type = null, string? Channel = null, string? Template = null);

public sealed class CreateAutomationValidator : AbstractValidator<CreateAutomationCommand>
{
    public CreateAutomationValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("ต้องระบุชื่อกติกา")
            .Must(name => name!.Trim().Length <= AutomationRule.NameMax)
            .WithMessage($"ชื่อกติกายาวได้ไม่เกิน {AutomationRule.NameMax} ตัวอักษร")
            .When(command => command.Name is not null, ApplyConditionTo.CurrentValidator);

        RuleFor(command => command.Trigger).NotNull().WithMessage("ต้องระบุ trigger");

        RuleFor(command => command.Trigger!.Type)
            .Must(type => WireEnum.TryParse<AutomationTrigger>(type, out _))
            .WithMessage($"trigger.type ต้องเป็น {WireEnum.Names<AutomationTrigger>()}")
            .When(command => command.Trigger is not null);

        RuleFor(command => command.Trigger)
            .Custom((trigger, context) => ValidateTiming(trigger, context))
            .When(command => command.Trigger is not null && WireEnum.TryParse<AutomationTrigger>(command.Trigger.Type, out _));

        When(command => command.Condition is not null, () =>
        {
            RuleFor(command => command.Condition!.BranchId).MaximumLength(40);
            RuleFor(command => command.Condition!.Species).MaximumLength(AutomationRule.FilterMax);
            RuleFor(command => command.Condition!.VaccineName).MaximumLength(AutomationRule.FilterMax);
            RuleFor(command => command.Condition!.ServiceId).MaximumLength(40);
            RuleFor(command => command.Condition)
                .Custom((condition, context) => ValidateCondition(context.InstanceToValidate.Trigger, condition!, context));
        });

        When(command => command.Action is not null, () =>
        {
            RuleFor(command => command.Action!.Type)
                .Must(type => type is null || type == AutomationWire.SendNotification)
                .WithMessage($"action.type ต้องเป็น {AutomationWire.SendNotification}");

            RuleFor(command => command.Action!.Channel)
                .Must(channel => channel is null || WireEnum.TryParse<AutomationChannel>(channel, out _))
                .WithMessage($"action.channel ต้องเป็น {WireEnum.Names<AutomationChannel>()}");

            RuleFor(command => command.Action!.Template)
                .Must((command, template) => template is null
                    || !WireEnum.TryParse<AutomationTrigger>(command.Trigger?.Type, out var trigger)
                    || template == AutomationMessages.TemplateFor(trigger))
                .WithMessage(command => WireEnum.TryParse<AutomationTrigger>(command.Trigger?.Type, out var trigger)
                    ? $"กติกานี้ส่งได้เฉพาะเทมเพลต {AutomationMessages.TemplateFor(trigger)}"
                    : "เทมเพลตไม่ตรงกับ trigger");
        });
    }

    private static void ValidateTiming(AutomationTriggerInput? trigger, ValidationContext<CreateAutomationCommand> context)
    {
        WireEnum.TryParse<AutomationTrigger>(trigger!.Type, out var type);

        if ((trigger.OffsetDays is null) == (trigger.OffsetHours is null))
        {
            context.AddFailure("trigger.offset_days", "ต้องระบุ offset_days หรือ offset_hours อย่างใดอย่างหนึ่ง");
            return;
        }

        if (trigger.OffsetHours is { } hours)
        {
            if (type == AutomationTrigger.VaccineDue)
            {
                context.AddFailure("trigger.offset_hours", "การเตือนวัคซีนนับเป็นวัน ใช้ offset_days");
            }
            else if (hours is < 1 or > AutomationRule.MaxOffsetHours)
            {
                context.AddFailure("trigger.offset_hours", $"offset_hours ต้องอยู่ระหว่าง 1 ถึง {AutomationRule.MaxOffsetHours}");
            }

            if (trigger.SendTime is not null)
            {
                context.AddFailure("trigger.send_time", "send_time ใช้คู่กับ offset_days เท่านั้น");
            }

            return;
        }

        var (min, max) = type == AutomationTrigger.VaccineDue
            ? (AutomationRule.MinVaccineOffsetDays, AutomationRule.MaxOffsetDays)
            : (0, AutomationRule.MaxVisitOffsetDays);

        if (trigger.OffsetDays < min || trigger.OffsetDays > max)
        {
            context.AddFailure("trigger.offset_days", $"offset_days ต้องอยู่ระหว่าง {min} ถึง {max}");
        }

        if (trigger.SendTime is not null && CreateAutomationHandler.ParseTime(trigger.SendTime) is null)
        {
            context.AddFailure("trigger.send_time", "send_time ต้องเป็นเวลาแบบ HH:mm");
        }
    }

    private static void ValidateCondition(
        AutomationTriggerInput? trigger,
        AutomationConditionInput condition,
        ValidationContext<CreateAutomationCommand> context)
    {
        if (!WireEnum.TryParse<AutomationTrigger>(trigger?.Type, out var type))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(condition.VaccineName) && type != AutomationTrigger.VaccineDue)
        {
            context.AddFailure("condition.vaccine_name", "เงื่อนไขชื่อวัคซีนใช้ได้กับการเตือนวัคซีนเท่านั้น");
        }

        if (!string.IsNullOrWhiteSpace(condition.ServiceId) && type == AutomationTrigger.VaccineDue)
        {
            context.AddFailure("condition.service_id", "เงื่อนไขบริการใช้ได้กับนัดและแผนการรักษาเท่านั้น");
        }

        if (condition.UnconfirmedOnly == true && type != AutomationTrigger.AppointmentUpcoming)
        {
            context.AddFailure("condition.unconfirmed_only", "ใช้ได้กับการเตือนนัดเท่านั้น");
        }
    }
}

/// <summary>
/// Creates a reminder rule.
/// <para>
/// <b>Off unless <c>enabled: true</c>.</b> A new rule messages real customers on its own at the
/// next tick, so the safe default is a rule that can be simulated first, then switched on with
/// <c>PATCH /staff/automations/{id}</c> (<c>docs/contract-extensions.md § 19</c>).
/// </para>
/// <para>
/// <b>Branch.</b> A rule belongs to one branch or to the whole clinic. A branch-bound member of
/// staff makes rules for a branch they work at, and with one branch that is the default; only a
/// caller the branch guard does not restrict may make a clinic-wide rule, since it reaches every
/// branch's customers.
/// </para>
/// </summary>
public sealed class CreateAutomationHandler(
    IAutomationRuleRepository rules,
    IBranchRepository branches,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<AutomationDto> HandleAsync(CreateAutomationCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        WireEnum.TryParse<AutomationTrigger>(command.Trigger!.Type, out var trigger);
        var channel = AutomationChannel.Auto;

        if (command.Action?.Channel is { } wireChannel)
        {
            WireEnum.TryParse(wireChannel, out channel);
        }

        var condition = command.Condition ?? new AutomationConditionInput();
        var branchId = await BranchForAsync(condition.BranchId, cancellationToken);

        if (condition.ServiceId is { Length: > 0 } serviceId
            && await catalog.FindServiceAsync(serviceId.Trim(), cancellationToken) is null)
        {
            throw Invalid("condition.service_id", "ไม่พบบริการนี้");
        }

        var rule = AutomationRule.Create(
            command.Name!,
            trigger,
            command.Trigger.OffsetDays,
            command.Trigger.OffsetHours,
            command.Trigger.SendTime is { } time ? ParseTime(time) : null,
            channel,
            AutomationMessages.TemplateFor(trigger),
            command.Enabled ?? false,
            staffId,
            branchId,
            condition.Species,
            condition.VaccineName,
            condition.ServiceId,
            condition.UnconfirmedOnly ?? false);

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            rules.Add(rule);

            audit.Record(new AuditEvent(
                AuditActions.AutomationCreated,
                AuditResources.Automation,
                rule.Id,
                BranchId: rule.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = rule.Name,
                    ["trigger"] = AutomationWire.ToWire(rule.Trigger),
                    ["offset_days"] = rule.OffsetDays,
                    ["offset_hours"] = rule.OffsetHours,
                    ["channel"] = AutomationWire.ToWire(rule.Channel),
                    ["template"] = rule.TemplateKey,
                    ["enabled"] = rule.Enabled,
                }));

            return Task.CompletedTask;
        }, cancellationToken);

        return AutomationDto.From(rule, AutomationStats.None);
    }

    /// <summary><c>HH:mm</c>, or null when it is not one.</summary>
    internal static TimeOnly? ParseTime(string value) =>
        TimeOnly.TryParseExact(value.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : null;

    private async Task<string?> BranchForAsync(string? requested, CancellationToken cancellationToken)
    {
        if (requested is { Length: > 0 } given)
        {
            var branchId = given.Trim();

            if ((await branches.MissingAsync([branchId], cancellationToken)).Count > 0)
            {
                throw new NotFoundException("branch", branchId);
            }

            branchGuard.EnsureCanAccess(branchId);
            return branchId;
        }

        if (branchGuard.VisibleBranchIds is not { } mine)
        {
            return null;
        }

        return mine.Count == 1
            ? mine.First()
            : throw Invalid("condition.branch_id", "ต้องระบุสาขาของกติกา กติกาทั้งคลินิกสร้างได้เฉพาะผู้ที่ไม่ถูกจำกัดสาขา");
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
