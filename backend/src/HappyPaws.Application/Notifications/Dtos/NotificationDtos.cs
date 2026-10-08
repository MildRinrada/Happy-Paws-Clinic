using HappyPaws.Application.Common;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Notifications.Dtos;

/// <summary>
/// One row of <c>GET /staff/notifications/log</c> — "ส่งสำเร็จ/ล้มเหลว · fallback ไป SMS เมื่อไหร่".
/// <c>FallbackFrom</c> names the channel that failed just before this attempt.
/// </summary>
public sealed record DeliveryDto(
    string Id,
    string CustomerId,
    string? CustomerName,
    string Template,
    string? Channel,
    string? Recipient,
    string Status,
    string? ErrorCode,
    string? FallbackFrom,
    string Source,
    string? BatchId,
    string? SentByStaffId,
    string? BranchId,
    DateTimeOffset AttemptedAt)
{
    public static DeliveryDto From(DeliveryLogRow row)
    {
        var delivery = row.Delivery;

        return new(
            delivery.Id,
            delivery.CustomerId,
            row.CustomerName,
            delivery.TemplateKey,
            delivery.Channel is { } channel ? WireEnum.Of(channel) : null,
            delivery.Recipient,
            WireEnum.Of(delivery.Status),
            delivery.ErrorCode,
            delivery.FallbackFrom is { } from ? WireEnum.Of(from) : null,
            WireEnum.Of(delivery.Source),
            delivery.BatchId,
            delivery.SentByStaffId,
            delivery.BranchId,
            delivery.CreatedAt);
    }
}

/// <summary>A value a template reads, and how long a staff send may make it.</summary>
public sealed record TemplateVariableDto(string Name, bool Required, int MaxLength);

/// <summary>The template's words: inbox title and email subject, LINE text (also the email body), SMS text.</summary>
public sealed record TemplateBodiesDto(string Title, string Line, string Sms);

/// <summary>
/// One entry of <c>GET /staff/notifications/templates</c>. <c>Purpose</c> is the consent it needs
/// (<c>service_messages</c> or <c>marketing</c>); <c>LineEnabled</c> is the switch set by
/// <c>PUT /staff/integrations/line</c>.
/// </summary>
public sealed record TemplateDto(
    string Key,
    string Name,
    string Purpose,
    bool StaffSendable,
    IReadOnlyList<TemplateVariableDto> Variables,
    bool LineEnabled,
    TemplateBodiesDto Bodies)
{
    public static TemplateDto From(NotificationTemplate template, bool lineEnabled) =>
        new(
            template.Key,
            template.Name,
            WireEnum.Of(ConsentGate.PurposeOf(template.Key)),
            template.StaffSendable,
            [.. template.Variables.Select(variable => new TemplateVariableDto(variable.Name, variable.Required, variable.MaxLength))],
            lineEnabled,
            new TemplateBodiesDto(template.Title, template.Line, template.Sms));
}

/// <summary>
/// What became of the message for one customer of a send. <c>Status</c> is <c>sent</c>,
/// <c>failed</c>, <c>skipped</c>, or <c>unknown</c> when no attempt was recorded.
/// </summary>
public sealed record RecipientResultDto(
    string CustomerId,
    string Status,
    string? Channel,
    string? ErrorCode,
    string? FallbackFrom);

/// <summary>A broadcast's monthly allowance after this send.</summary>
public sealed record BroadcastQuotaDto(int Limit, int Used, int Remaining);

/// <summary>
/// The answer to <c>POST /staff/notifications/send</c> and <c>POST /staff/integrations/line/broadcast</c>:
/// counts, one line per customer, and the batch id to find the rows in the log.
/// </summary>
public sealed record SendResultDto(
    string BatchId,
    int Sent,
    int Failed,
    int Skipped,
    IReadOnlyList<RecipientResultDto> Results,
    BroadcastQuotaDto? Quota = null)
{
    /// <summary>
    /// Folds a batch's rows into one outcome per customer: sent if any attempt went out, failed if
    /// one was tried and none went out, skipped if nothing was tried.
    /// </summary>
    public static SendResultDto Summarise(
        string batchId,
        IReadOnlyList<string> customerIds,
        IReadOnlyList<NotificationDelivery> rows,
        BroadcastQuotaDto? quota = null)
    {
        var results = customerIds.Select(customerId =>
        {
            var mine = rows.Where(row => row.CustomerId == customerId).ToList();
            var sent = mine.LastOrDefault(row => row.Status == DeliveryStatus.Sent);
            var failed = mine.LastOrDefault(row => row.Status == DeliveryStatus.Failed);
            var skipped = mine.LastOrDefault(row => row.Status == DeliveryStatus.Skipped);
            var shown = sent ?? failed ?? skipped;

            return shown is null
                ? new RecipientResultDto(customerId, "unknown", null, null, null)
                : new RecipientResultDto(
                    customerId,
                    WireEnum.Of(shown.Status),
                    shown.Channel is { } channel ? WireEnum.Of(channel) : null,
                    shown.ErrorCode,
                    shown.FallbackFrom is { } from ? WireEnum.Of(from) : null);
        }).ToList();

        return new SendResultDto(
            batchId,
            results.Count(result => result.Status == "sent"),
            results.Count(result => result.Status == "failed"),
            results.Count(result => result.Status is "skipped" or "unknown"),
            results,
            quota);
    }
}
