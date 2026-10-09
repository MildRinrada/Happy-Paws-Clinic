using HappyPaws.Application.Common;
using HappyPaws.Domain.Messaging;

namespace HappyPaws.Application.Messaging.Dtos;

/// <summary>
/// One message of the customer's own thread, as the portal shows it
/// (<c>docs/contract-extensions.md § 24</c>).
/// <para>
/// Narrower than the staff view on purpose: no staff id and no delivery outcome. <c>channel</c> is
/// where the customer wrote from (<c>line</c>, <c>portal</c>...), and only on their own messages.
/// <c>sent_by_name</c> is the display name of the member of staff who answered, only on the
/// clinic's messages; the page says "คลินิก" when it is absent.
/// </para>
/// </summary>
public sealed record PortalThreadMessageDto(
    string Id,
    string Direction,
    string? Channel,
    string Kind,
    string? Text,
    DateTimeOffset SentAt,
    string? SentByName)
{
    public static PortalThreadMessageDto From(ThreadMessage message, string? sentByName)
    {
        var inbound = message.Direction == MessageDirection.Inbound;

        return new(
            message.Id,
            WireEnum.Of(message.Direction),
            inbound && message.Channel is { } channel ? WireEnum.Of(channel) : null,
            WireEnum.Of(message.Kind),
            message.Text,
            message.SentAt,
            inbound ? null : sentByName);
    }

    public static PortalThreadMessageDto From(ThreadMessageRow row) => From(row.Message, row.SentByName);
}

/// <summary>
/// <c>GET /portal/thread</c>: the signed-in customer's thread and one page of its messages, newest
/// first. <c>Id</c> and <c>Status</c> are null until the customer writes for the first time.
/// </summary>
public sealed record PortalThreadDto(
    string? Id,
    string? Status,
    IReadOnlyList<PortalThreadMessageDto> Data,
    string? NextCursor);
