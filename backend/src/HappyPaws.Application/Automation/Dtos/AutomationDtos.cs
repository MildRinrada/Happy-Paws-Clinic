using System.Globalization;
using System.Text.Json.Serialization;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation.Dtos;

/// <summary>A rule as <c>GET</c> and <c>POST /staff/automations</c> return it.</summary>
/// <param name="Id"><c>atm_...</c>.</param>
/// <param name="Version">For <c>If-Match</c> on an edit or a delete (<c>docs/contract-extensions.md § 19</c>).</param>
/// <param name="Name">What staff call it.</param>
/// <param name="Enabled">Whether the scheduler runs it.</param>
/// <param name="Trigger">What it watches and when it sends.</param>
/// <param name="Condition">Which subjects count.</param>
/// <param name="Action">What it sends, and how.</param>
/// <param name="Stats">"สถิติการยิงย้อนหลัง".</param>
/// <param name="CreatedByStaffId">Absent for a starter rule the clinic was given.</param>
/// <param name="CreatedAt">When it was made.</param>
public sealed record AutomationDto(
    string Id,
    uint Version,
    string Name,
    bool Enabled,
    AutomationTriggerDto Trigger,
    AutomationConditionDto Condition,
    AutomationActionDto Action,
    AutomationStatsDto Stats,
    string? CreatedByStaffId,
    DateTimeOffset CreatedAt)
{
    public static AutomationDto From(AutomationRule rule, AutomationStats stats) => new(
        rule.Id,
        rule.Version,
        rule.Name,
        rule.Enabled,
        new AutomationTriggerDto(
            AutomationWire.ToWire(rule.Trigger),
            rule.OffsetDays,
            rule.OffsetHours,
            rule.SendTime?.ToString("HH:mm", CultureInfo.InvariantCulture)),
        new AutomationConditionDto(rule.BranchId, rule.Species, rule.VaccineName, rule.ServiceId, rule.UnconfirmedOnly),
        new AutomationActionDto(AutomationWire.SendNotification, AutomationWire.ToWire(rule.Channel), rule.TemplateKey),
        new AutomationStatsDto(stats.Total, stats.Sent, stats.Skipped, stats.Failed, stats.Lost, stats.Recent, stats.LastFiredAt),
        rule.CreatedByStaffId,
        rule.CreatedAt);
}

/// <param name="Type"><c>vaccine_due</c>, <c>appointment_upcoming</c> or <c>treatment_plan_session</c>.</param>
/// <param name="OffsetDays">Days before the subject's day.</param>
/// <param name="OffsetHours">Hours before the subject's start.</param>
/// <param name="SendTime">Clinic time of day, <c>HH:mm</c>, with <paramref name="OffsetDays"/>.</param>
public sealed record AutomationTriggerDto(string Type, int? OffsetDays, int? OffsetHours, string? SendTime);

/// <param name="BranchId">The branch it covers; absent for the whole clinic.</param>
/// <param name="Species">Only this species.</param>
/// <param name="VaccineName">Only this vaccine.</param>
/// <param name="ServiceId">Only this service.</param>
/// <param name="UnconfirmedOnly">Only appointments not yet confirmed.</param>
public sealed record AutomationConditionDto(
    string? BranchId,
    string? Species,
    string? VaccineName,
    string? ServiceId,
    bool UnconfirmedOnly);

/// <param name="Type">Always <c>send_notification</c>.</param>
/// <param name="Channel"><c>auto</c>, <c>line</c>, <c>sms</c> or <c>email</c>.</param>
/// <param name="Template">The template key it sends.</param>
public sealed record AutomationActionDto(string Type, string Channel, string Template);

/// <param name="Total">Reminders it has claimed.</param>
/// <param name="Sent">Accepted on a channel.</param>
/// <param name="Skipped">Not attempted, such as no consent.</param>
/// <param name="Failed">Refused on every channel.</param>
/// <param name="Lost">Claimed by a worker that stopped before sending.</param>
/// <param name="Last30Days">Claimed in the last 30 days.</param>
/// <param name="LastFiredAt">The latest one.</param>
public sealed record AutomationStatsDto(
    int Total,
    int Sent,
    int Skipped,
    int Failed,
    int Lost,
    [property: JsonPropertyName("last_30_days")] int Last30Days,
    DateTimeOffset? LastFiredAt);
