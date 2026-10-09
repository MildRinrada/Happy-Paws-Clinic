using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.SendNotification;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Notifications.BroadcastToSegment;

/// <summary>
/// <c>POST /staff/integrations/line/broadcast</c> — "ประกาศถึงกลุ่มลูกค้าที่คัดไว้ · จำกัดโควตา".
/// <para>
/// The group is either a list of customers in the request or a saved segment
/// (<c>POST /staff/segments</c>), exactly one of the two. A segment is worked out at send time and
/// narrowed to its members who agreed to marketing and have a LINE address, so its size is the
/// number of people who can actually receive it. See <c>docs/features/line.md</c>.
/// </para>
/// </summary>
public sealed record BroadcastToSegmentCommand(IReadOnlyList<string> CustomerIds, string Message, string? SegmentId = null);

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
            .NotEmpty().WithMessage("ต้องเลือกลูกค้าอย่างน้อยหนึ่งคน หรือเลือกกลุ่มลูกค้า")
            .When(command => command.SegmentId is null);

        RuleFor(command => command.CustomerIds)
            .Must(ids => ids is null || ids.Count <= MaxRecipients)
            .WithMessage($"ประกาศได้ไม่เกิน {MaxRecipients} คนต่อครั้ง");

        RuleFor(command => command.CustomerIds)
            .Empty().WithMessage("เลือกกลุ่มลูกค้าหรือรายชื่อลูกค้าอย่างใดอย่างหนึ่ง")
            .When(command => command.SegmentId is not null);

        RuleFor(command => command.SegmentId).NotEmpty().MaximumLength(40).When(command => command.SegmentId is not null);

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
    ISegmentRepository segments,
    ISegmentQuery segmentQuery,
    BranchGuard branchGuard,
    ClinicDays clinicDays,
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

        var customerIds = command.SegmentId is { } segmentId
            ? await SegmentRecipientsAsync(segmentId, cancellationToken)
            : command.CustomerIds.Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();

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
                    ["segment_id"] = command.SegmentId,
                    ["recipients"] = customerIds.Count,
                    ["sent"] = result.Sent,
                    ["failed"] = result.Failed,
                    ["skipped"] = result.Skipped,
                }),
            cancellationToken);

        return result;
    }

    /// <summary>
    /// The segment's members who can receive a LINE announcement, worked out now. One more than the
    /// cap is asked for, so a group that is too big is refused whole instead of cut off silently.
    /// </summary>
    private async Task<List<string>> SegmentRecipientsAsync(string segmentId, CancellationToken cancellationToken)
    {
        var segment = await segments.FindByIdAsync(segmentId, cancellationToken)
            ?? throw new NotFoundException("segment", segmentId);

        if (segment.BranchId is { } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }
        else if (branchGuard.VisibleBranchIds is not null)
        {
            throw new ForbiddenException("a branch-bound staff member cannot broadcast to a segment over every branch");
        }

        var members = await segmentQuery.ReachableMembersAsync(
            segment.Criteria, clinicDays.Today, BroadcastToSegmentValidator.MaxRecipients + 1, cancellationToken);

        if (members.Count == 0)
        {
            throw Invalid("ไม่มีลูกค้าในกลุ่มนี้ที่ยอมรับข่าวสารและมี LINE");
        }

        if (members.Count > BroadcastToSegmentValidator.MaxRecipients)
        {
            throw Invalid($"กลุ่มนี้มีผู้รับเกิน {BroadcastToSegmentValidator.MaxRecipients} คน แบ่งกลุ่มให้เล็กลงก่อนส่ง");
        }

        return [.. members];
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["segment_id"] = [message] });

    private DateTimeOffset StartOfMonth()
    {
        var local = clock.NowInClinicTime();
        return new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, local.Offset);
    }
}
