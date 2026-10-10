using HappyPaws.Application.Notifications;

namespace HappyPaws.Application.Automation.Dtos;

/// <summary>One reminder a rule would send: a row of the simulation and of the upcoming list.</summary>
/// <param name="AutomationId">The rule.</param>
/// <param name="AutomationName">Its name.</param>
/// <param name="SendAt">When it goes out.</param>
/// <param name="DueNow">True when its time has come: the next run sends it.</param>
/// <param name="Status"><c>would_send</c>, <c>would_skip</c> or <c>already_sent</c>.</param>
/// <param name="SkipReason">Why it would be skipped: <c>no_consent</c>.</param>
/// <param name="CustomerId">Who is told.</param>
/// <param name="CustomerName">Their name.</param>
/// <param name="PetId">The patient.</param>
/// <param name="PetName">Its name.</param>
/// <param name="BranchId">The branch it belongs to, when one.</param>
/// <param name="Subject">What it is about.</param>
/// <param name="Template">The template key.</param>
/// <param name="Channel"><c>auto</c>, <c>line</c>, <c>sms</c> or <c>email</c>.</param>
/// <param name="Message">The words, as the web inbox would show them.</param>
public sealed record ReminderDto(
    string AutomationId,
    string AutomationName,
    DateTimeOffset SendAt,
    bool DueNow,
    string Status,
    string? SkipReason,
    string CustomerId,
    string CustomerName,
    string PetId,
    string PetName,
    string? BranchId,
    ReminderSubjectDto Subject,
    string Template,
    string Channel,
    ReminderMessageDto Message)
{
    public static ReminderDto From(AutomationOccurrence occurrence)
    {
        var subject = occurrence.Subject;
        var text = NotificationText.Render(occurrence.Request);

        return new ReminderDto(
            occurrence.Rule.Id,
            occurrence.Rule.Name,
            occurrence.SendAt,
            occurrence.DueNow,
            AutomationWire.ToWire(occurrence.Status),
            occurrence.SkipReason,
            subject.CustomerId,
            subject.CustomerName,
            subject.PetId,
            subject.PetName,
            subject.BranchId,
            new ReminderSubjectDto(
                AutomationWire.ToWire(subject.Kind),
                subject.SubjectId,
                subject.Day,
                subject.StartsAt,
                subject.Label,
                subject.AppointmentId),
            occurrence.Rule.TemplateKey,
            AutomationWire.ToWire(occurrence.Rule.Channel),
            new ReminderMessageDto(text.Title, text.Body));
    }
}

/// <param name="Type"><c>vaccination</c>, <c>appointment</c>, <c>treatment_plan_session</c> or <c>customer</c>.</param>
/// <param name="Id">The record; a session is <c>{plan id}:{number}</c>; a lapsed customer is the customer id.</param>
/// <param name="DueOn">Its clinic day; for a lapsed customer, the day they would usually have been back by.</param>
/// <param name="StartsAt">Its start, when it has one.</param>
/// <param name="Label">The vaccine or the plan.</param>
/// <param name="AppointmentId">The appointment, when there is one.</param>
public sealed record ReminderSubjectDto(
    string Type,
    string Id,
    DateOnly DueOn,
    DateTimeOffset? StartsAt,
    string? Label,
    string? AppointmentId);

/// <param name="Title">The heading.</param>
/// <param name="Body">The text.</param>
public sealed record ReminderMessageDto(string Title, string Body);

/// <summary><c>POST /staff/automations/{id}/simulate</c>.</summary>
/// <param name="AutomationId">The rule.</param>
/// <param name="Enabled">Whether it is switched on.</param>
/// <param name="EvaluatedAt">The instant it was evaluated at.</param>
/// <param name="From">Earliest send time looked at (the catch-up window).</param>
/// <param name="To">Latest send time looked at.</param>
/// <param name="DueNow">Reminders a run at this instant would hand to the dispatcher.</param>
/// <param name="WouldSend">Rows that may be sent.</param>
/// <param name="WouldSkip">Rows the consent gate would stop.</param>
/// <param name="AlreadySent">Rows this rule already sent.</param>
/// <param name="Truncated">True when there were more rows than returned.</param>
/// <param name="Recipients">Each reminder, earliest first.</param>
public sealed record SimulationDto(
    string AutomationId,
    bool Enabled,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset From,
    DateTimeOffset To,
    int DueNow,
    int WouldSend,
    int WouldSkip,
    int AlreadySent,
    bool Truncated,
    IReadOnlyList<ReminderDto> Recipients);

/// <summary><c>GET /staff/reminders/upcoming</c>: "คิวข้อความที่จะถูกส่งใน 7 วันข้างหน้า".</summary>
/// <param name="From">Earliest send time looked at.</param>
/// <param name="To">Latest send time looked at.</param>
/// <param name="Total">Reminders found, before truncation.</param>
/// <param name="Truncated">True when there were more than returned.</param>
/// <param name="Data">Each reminder still to go, earliest first.</param>
public sealed record UpcomingRemindersDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int Total,
    bool Truncated,
    IReadOnlyList<ReminderDto> Data);
