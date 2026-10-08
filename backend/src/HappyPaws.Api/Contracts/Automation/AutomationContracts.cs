using HappyPaws.Application.Automation.CreateAutomation;

namespace HappyPaws.Api.Contracts.Automation;

/// <summary>
/// <c>POST /staff/automations</c> — a rule as <c>trigger + condition + action</c>:
/// <code>
/// {
///   "name": "เตือนวัคซีนล่วงหน้า 14 วัน",
///   "trigger":   { "type": "vaccine_due", "offset_days": 14, "send_time": "09:00" },
///   "condition": { "branch_id": "brn_...", "species": "สุนัข", "vaccine_name": "Rabies" },
///   "action":    { "type": "send_notification", "channel": "line" },
///   "enabled": false
/// }
/// </code>
/// <c>trigger.type</c> is <c>vaccine_due</c>, <c>appointment_upcoming</c> or
/// <c>treatment_plan_session</c>; give <c>offset_days</c> (with an optional <c>send_time</c>) or
/// <c>offset_hours</c>. <c>condition</c> and <c>action</c> are optional. <c>enabled</c> is false
/// unless sent.
/// </summary>
public sealed record CreateAutomationRequest(
    string? Name = null,
    AutomationTriggerRequest? Trigger = null,
    AutomationConditionRequest? Condition = null,
    AutomationActionRequest? Action = null,
    bool? Enabled = null)
{
    public CreateAutomationCommand ToCommand() => new(
        Name,
        Trigger is null ? null : new AutomationTriggerInput(Trigger.Type, Trigger.OffsetDays, Trigger.OffsetHours, Trigger.SendTime),
        Condition is null
            ? null
            : new AutomationConditionInput(Condition.BranchId, Condition.Species, Condition.VaccineName, Condition.ServiceId, Condition.UnconfirmedOnly),
        Action is null ? null : new AutomationActionInput(Action.Type, Action.Channel, Action.Template),
        Enabled);
}

/// <summary>What the rule watches, and when it sends.</summary>
public sealed record AutomationTriggerRequest(string? Type = null, int? OffsetDays = null, int? OffsetHours = null, string? SendTime = null);

/// <summary>Which subjects count.</summary>
public sealed record AutomationConditionRequest(
    string? BranchId = null,
    string? Species = null,
    string? VaccineName = null,
    string? ServiceId = null,
    bool? UnconfirmedOnly = null);

/// <summary>What the rule sends.</summary>
public sealed record AutomationActionRequest(string? Type = null, string? Channel = null, string? Template = null);

/// <summary><c>POST /staff/automations/{id}/simulate</c>. The body is optional: <c>{ "days": 7 }</c>.</summary>
public sealed record SimulateAutomationRequest(int? Days = null);
