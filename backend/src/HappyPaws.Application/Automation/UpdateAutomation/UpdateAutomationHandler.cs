using System.Globalization;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Automation.CreateAutomation;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.ListAutomations;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation.UpdateAutomation;

/// <summary>
/// <c>PATCH /staff/automations/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 19</c>. Every part is optional and an absent one is left alone.
/// </summary>
/// <param name="AutomationId">The rule.</param>
/// <param name="Name">A new name.</param>
/// <param name="Trigger">
/// New timing. <c>type</c> may be sent but must be the rule's own: what a rule watches never changes.
/// Sending <c>offset_days</c> or <c>offset_hours</c> replaces the timing; sending only
/// <c>send_time</c> moves the hour of a day-based rule.
/// </param>
/// <param name="Condition">
/// Conditions to change. An absent field is kept, an empty string clears it; an empty
/// <c>branch_id</c> makes the rule clinic-wide, which only an unrestricted caller may do.
/// </param>
/// <param name="Action">A new <c>channel</c>.</param>
/// <param name="Enabled">Switches the rule on or off.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdateAutomationCommand(
    string AutomationId,
    string? Name = null,
    AutomationTriggerInput? Trigger = null,
    AutomationConditionInput? Condition = null,
    AutomationActionInput? Action = null,
    bool? Enabled = null,
    uint? IfMatchVersion = null);

/// <summary>
/// Edits a reminder rule, or switches it on or off.
/// <para>
/// <b>The same checks as create, not a copy of them.</b> The patch is laid over the rule as it
/// stands, and the result is put through <see cref="CreateAutomationValidator"/> as though it were a
/// new rule, so a rule that could not be created cannot be reached by editing either, and an error
/// names the same field (<c>trigger.offset_days</c>) either way.
/// </para>
/// <para>
/// <b>No second reminder after an edit.</b> The "already sent" key is
/// <c>(rule, subject, due date)</c>. An edit changes none of the three: the trigger is fixed, and
/// the timing is not part of the key. A subject this rule has already reminded stays reminded, so
/// moving "24 hours before" to "2 hours before" never sends the same visit a second message.
/// </para>
/// <para>
/// <b>Branch.</b> As for create: a branch-bound caller edits rules of their branches only, cannot
/// touch a clinic-wide rule, and cannot make one.
/// </para>
/// </summary>
public sealed class UpdateAutomationHandler(
    AutomationAccess access,
    IAutomationDispatchRepository dispatches,
    IBranchRepository branches,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<AutomationDto> HandleAsync(UpdateAutomationCommand command, CancellationToken cancellationToken = default)
    {
        AutomationRule? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var rule = await access.LoadForChangeAsync(command.AutomationId, token);

            ConcurrencyGuard.Ensure(rule, command.IfMatchVersion);

            var trigger = AutomationWire.ToWire(rule.Trigger);

            if (command.Trigger?.Type is { } type && type != trigger)
            {
                throw Invalid("trigger.type", "เปลี่ยนเรื่องที่กติกาเฝ้าดูไม่ได้ ถ้าต้องการเตือนเรื่องอื่น ให้สร้างกติกาใหม่");
            }

            var merged = Merge(rule, command, trigger);

            // The create rules, on the rule as it would stand after the edit.
            await validator.ValidateAsync(merged, token);

            var condition = merged.Condition!;
            var branchId = await BranchForAsync(rule, command.Condition?.BranchId, token);

            if (condition.ServiceId is { Length: > 0 } serviceId
                && serviceId != rule.ServiceId
                && await catalog.FindServiceAsync(serviceId, token) is null)
            {
                throw Invalid("condition.service_id", "ไม่พบบริการนี้");
            }

            WireEnum.TryParse<AutomationChannel>(merged.Action!.Channel, out var channel);
            var before = Snapshot(rule);

            rule.Revise(
                merged.Name!,
                merged.Trigger!.OffsetDays,
                merged.Trigger.OffsetHours,
                merged.Trigger.SendTime is { } time ? CreateAutomationHandler.ParseTime(time) : null,
                channel,
                branchId,
                condition.Species,
                condition.VaccineName,
                condition.ServiceId,
                condition.UnconfirmedOnly ?? false);

            if (command.Enabled is { } enabled)
            {
                rule.SetEnabled(enabled);
            }

            var changed = Changed(before, Snapshot(rule));

            if (changed.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.AutomationUpdated,
                    AuditResources.Automation,
                    rule.Id,
                    BranchId: rule.BranchId ?? before.BranchId,
                    Details: new Dictionary<string, object?>
                    {
                        ["name"] = rule.Name,
                        ["changed"] = changed,
                        ["enabled"] = rule.Enabled,
                        ["offset_days"] = rule.OffsetDays,
                        ["offset_hours"] = rule.OffsetHours,
                        ["channel"] = AutomationWire.ToWire(rule.Channel),
                    }));
            }

            updated = rule;
        }, cancellationToken);

        var stats = await dispatches.StatsAsync(
            [updated!.Id],
            clock.UtcNow.AddDays(-ListAutomationsHandler.RecentDays),
            cancellationToken);

        return AutomationDto.From(updated, stats.GetValueOrDefault(updated.Id, AutomationStats.None));
    }

    /// <summary>The rule as it would be after the patch, in the shape create validates.</summary>
    private static CreateAutomationCommand Merge(AutomationRule rule, UpdateAutomationCommand command, string trigger)
    {
        var patch = command.Trigger;
        int? offsetDays = rule.OffsetDays;
        int? offsetHours = rule.OffsetHours;
        var sendTime = rule.SendTime?.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (patch is not null)
        {
            if (patch.OffsetDays is not null || patch.OffsetHours is not null)
            {
                // New timing, whole: both offsets as sent, so sending both is refused as on create.
                offsetDays = patch.OffsetDays;
                offsetHours = patch.OffsetHours;
                sendTime = patch.SendTime;
            }
            else if (patch.SendTime is not null)
            {
                sendTime = patch.SendTime;
            }
        }

        var condition = command.Condition;

        return new CreateAutomationCommand(
            command.Name ?? rule.Name,
            new AutomationTriggerInput(trigger, offsetDays, offsetHours, sendTime),
            new AutomationConditionInput(
                // Branch is checked on its own below; here it only has to fit the column.
                BranchId: null,
                Species: Pick(condition?.Species, rule.Species),
                VaccineName: Pick(condition?.VaccineName, rule.VaccineName),
                ServiceId: Pick(condition?.ServiceId, rule.ServiceId),
                UnconfirmedOnly: condition?.UnconfirmedOnly ?? rule.UnconfirmedOnly),
            new AutomationActionInput(
                command.Action?.Type,
                command.Action?.Channel ?? AutomationWire.ToWire(rule.Channel),
                command.Action?.Template));
    }

    /// <summary>Absent keeps, an empty string clears, anything else replaces.</summary>
    private static string? Pick(string? sent, string? current) => sent switch
    {
        null => current,
        _ when string.IsNullOrWhiteSpace(sent) => null,
        _ => sent.Trim(),
    };

    private async Task<string?> BranchForAsync(AutomationRule rule, string? requested, CancellationToken cancellationToken)
    {
        if (requested is null)
        {
            return rule.BranchId;
        }

        if (string.IsNullOrWhiteSpace(requested))
        {
            return branchGuard.VisibleBranchIds is null
                ? null
                : throw Invalid("condition.branch_id", "กติกาทั้งคลินิกตั้งได้เฉพาะผู้ที่ไม่ถูกจำกัดสาขา");
        }

        var branchId = requested.Trim();

        if (branchId.Length > 40 || (await branches.MissingAsync([branchId], cancellationToken)).Count > 0)
        {
            throw new NotFoundException("branch", branchId);
        }

        branchGuard.EnsureCanAccess(branchId);
        return branchId;
    }

    private sealed record RuleState(
        string Name,
        bool Enabled,
        int? OffsetDays,
        int? OffsetHours,
        TimeOnly? SendTime,
        AutomationChannel Channel,
        string? BranchId,
        string? Species,
        string? VaccineName,
        string? ServiceId,
        bool UnconfirmedOnly);

    private static RuleState Snapshot(AutomationRule rule) => new(
        rule.Name,
        rule.Enabled,
        rule.OffsetDays,
        rule.OffsetHours,
        rule.SendTime,
        rule.Channel,
        rule.BranchId,
        rule.Species,
        rule.VaccineName,
        rule.ServiceId,
        rule.UnconfirmedOnly);

    /// <summary>The wire names of what changed, for the audit row. Values are on the rule itself.</summary>
    private static List<string> Changed(RuleState before, RuleState after)
    {
        var changed = new List<string>();

        void Check(bool differs, string field)
        {
            if (differs)
            {
                changed.Add(field);
            }
        }

        Check(before.Name != after.Name, "name");
        Check(before.Enabled != after.Enabled, "enabled");
        Check(
            before.OffsetDays != after.OffsetDays || before.OffsetHours != after.OffsetHours || before.SendTime != after.SendTime,
            "trigger");
        Check(before.Channel != after.Channel, "action.channel");
        Check(before.BranchId != after.BranchId, "condition.branch_id");
        Check(before.Species != after.Species, "condition.species");
        Check(before.VaccineName != after.VaccineName, "condition.vaccine_name");
        Check(before.ServiceId != after.ServiceId, "condition.service_id");
        Check(before.UnconfirmedOnly != after.UnconfirmedOnly, "condition.unconfirmed_only");

        return changed;
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
