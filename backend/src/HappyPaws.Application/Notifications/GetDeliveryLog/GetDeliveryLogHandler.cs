using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Notifications.GetDeliveryLog;

/// <summary>
/// <c>GET /staff/notifications/log</c> — "ส่งสำเร็จ/ล้มเหลว · fallback ไป SMS เมื่อไหร่". Every
/// filter is optional: <c>status</c>, <c>channel</c>, <c>source</c>, <c>customer_id</c>,
/// <c>template</c>, <c>batch_id</c>.
/// </summary>
public sealed record GetDeliveryLogQuery(
    string? Status = null,
    string? Channel = null,
    string? Source = null,
    string? CustomerId = null,
    string? Template = null,
    string? BatchId = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The delivery log, newest first.
/// <para>
/// Restricted to the caller's branches the way the other staff lists are, with one difference:
/// rows that belong to no branch (a portal invitation, a broadcast, a send by an unrestricted
/// member) are shown to every reader. They are clinic-wide by nature, and hiding them from a
/// branch would hide the answer to "why did this owner get that message".
/// </para>
/// <para>
/// Not audited. A row names a customer and a template, never the message's content, and the
/// customer's contact details are masked.
/// </para>
/// </summary>
public sealed class GetDeliveryLogHandler(IDeliveryLog deliveryLog, BranchGuard branchGuard)
{
    public async Task<CursorPage<DeliveryDto>> HandleAsync(
        GetDeliveryLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        var status = Parse<DeliveryStatus>(query.Status, "status", errors);
        var channel = Parse<DeliveryChannel>(query.Channel, "channel", errors);
        var source = Parse<DeliverySource>(query.Source, "source", errors);

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var page = await deliveryLog.ListAsync(
            new DeliveryLogFilter(
                status,
                channel,
                source,
                Blank(query.CustomerId),
                Blank(query.Template),
                Blank(query.BatchId),
                branchGuard.VisibleBranchIds),
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<DeliveryDto>([.. page.Data.Select(DeliveryDto.From)], page.NextCursor);
    }

    private static TEnum? Parse<TEnum>(string? value, string field, Dictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (WireEnum.TryParse<TEnum>(value, out var parsed))
        {
            return parsed;
        }

        errors[field] = [$"{field} ต้องเป็น {WireEnum.Names<TEnum>()}"];
        return null;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
