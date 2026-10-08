using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.SendNotification;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Notifications.BroadcastToSegment;

/// <summary>
/// <c>POST /staff/integrations/line/broadcast</c> — "ประกาศถึงกลุ่มลูกค้าที่คัดไว้ · จำกัดโควตา".
/// <para>
/// The group is the list of customers in the request. The contract's saved segments
/// (<c>POST /staff/segments</c>) belong to the CRM module, which does not exist yet; when it
/// does, a <c>segment_id</c> can resolve to this same list. See <c>docs/features/line.md</c>.
/// </para>
/// </summary>
public sealed record BroadcastToSegmentCommand(IReadOnlyList<string> CustomerIds, string Message);

public sealed class BroadcastToSegmentValidator : AbstractValidator<BroadcastToSegmentCommand>
{
    /// <summary>
    /// Each customer is one push, sent one after another inside the request. Two hundred keeps a
    /// broadcast well inside a request timeout; a bigger audience is several broadcasts.
    /// </summary>
    public const int MaxRecipients = 200;

    public const int MaxMessageLength = 1_000;

    public BroadcastToSegmentValidator()
    {
        RuleFor(command => command.CustomerIds)
            .NotEmpty().WithMessage("ต้องเลือกลูกค้าอย่างน้อยหนึ่งคน")
            .Must(ids => ids is null || ids.Count <= MaxRecipients)
            .WithMessage($"ประกาศได้ไม่เกิน {MaxRecipients} คนต่อครั้ง");

        RuleForEach(command => command.CustomerIds).NotEmpty().MaximumLength(40);

        RuleFor(command => command.Message)
            .NotEmpty().WithMessage("ต้องมีข้อความประกาศ")
            .MaximumLength(MaxMessageLength);
    }
}

/// <summary>
/// Sends an announcement to a list of customers on LINE.
/// <para>
/// <b>Marketing, and gated as marketing.</b> Each copy goes through the dispatcher under the
/// <c>clinic.announcement</c> template, so only customers who have agreed to marketing receive it;
/// everyone else is recorded as skipped for <c>no_consent</c>. LINE only, no fallback: an
/// announcement nobody asked for is not worth an SMS credit, and an owner without LINE is simply
/// skipped.
/// </para>
/// <para>
/// <b>The quota is counted from the delivery log</b>, per calendar month in the clinic's time zone,
/// against the limit set on the LINE settings. The check is made before sending, on the number of
/// customers asked for, so a broadcast is refused whole rather than cut off part way. Two
/// broadcasts started at the same moment can both pass the check; the limit is a budget, not a
/// lock.
/// </para>
/// </summary>
public sealed class BroadcastToSegmentHandler(
    INotificationDispatcher dispatcher,
    ICustomerRepository customers,
    IDeliveryLog deliveryLog,
    IClinicSettingsStore settings,
    ILineGateway line,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IClock clock,
    RequestValidator validator,
    ILogger<BroadcastToSegmentHandler> logger)
{
    public async Task<SendResultDto> HandleAsync(
        BroadcastToSegmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();

        if (!(await line.ConnectionAsync(cancellationToken)).CanPush)
        {
            throw new ConflictException(ErrorCodes.LineNotConfigured, "ยังไม่ได้ตั้งค่า LINE หรือปิดการส่งทาง LINE ไว้")
            {
                MessageEn = "LINE has no access token or is switched off.",
            };
        }

        var customerIds = command.CustomerIds.Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();
        await SendNotificationHandler.EnsureCustomersExistAsync(customers, customerIds, cancellationToken);

        var limit = (await settings.ReadAsync(
            LineChannelSettings.SingletonId, LineChannelSettings.CreateDefault, cancellationToken)).BroadcastMonthlyLimit;
        var used = await deliveryLog.CountSentSinceAsync(
            DeliverySource.Broadcast, DeliveryChannel.Line, StartOfMonth(), cancellationToken);

        if (used + customerIds.Count > limit)
        {
            throw new ConflictException(ErrorCodes.QuotaExceeded, $"เกินโควตาประกาศของเดือนนี้ (ใช้ไป {used} จาก {limit})")
            {
                MessageEn = "This broadcast would pass the monthly quota.",
                Details = new Dictionary<string, object>
                {
                    ["limit"] = limit,
                    ["used"] = used,
                    ["requested"] = customerIds.Count,
                },
            };
        }

        var batchId = EntityId.New(EntityId.NotificationBatch);
        var data = new Dictionary<string, string> { ["message"] = command.Message.Trim() };

        foreach (var customerId in customerIds)
        {
            try
            {
                await dispatcher.SendAsync(
                    new NotificationRequest(customerId, NotificationTemplates.ClinicAnnouncement, data)
                    {
                        Channel = "line",
                        Source = DeliverySource.Broadcast,
                        BatchId = batchId,
                        SentByStaffId = staffId,
                    },
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Broadcast {BatchId} failed for customer {CustomerId}", batchId, customerId);
            }
        }

        var rows = await deliveryLog.ForBatchAsync(batchId, cancellationToken);
        var sent = rows.Count(row => row.Status == DeliveryStatus.Sent);
        var result = SendResultDto.Summarise(
            batchId, customerIds, rows, new BroadcastQuotaDto(limit, used + sent, Math.Max(0, limit - used - sent)));

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.NotificationBroadcast,
                AuditResources.NotificationBatch,
                batchId,
                Details: new Dictionary<string, object?>
                {
                    ["recipients"] = customerIds.Count,
                    ["sent"] = result.Sent,
                    ["failed"] = result.Failed,
                    ["skipped"] = result.Skipped,
                }),
            cancellationToken);

        return result;
    }

    private DateTimeOffset StartOfMonth()
    {
        var local = clock.NowInClinicTime();
        return new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, local.Offset);
    }
}
