using HappyPaws.Application.Common;
using HappyPaws.Domain.Messaging;

namespace HappyPaws.Application.Messaging.Dtos;

/// <summary>
/// What became of a reply: <c>status</c> is <c>sent</c>, <c>failed</c> or <c>skipped</c> (as in the
/// delivery log), <c>channel</c> is the one that carried it or was tried, and <c>fallback_from</c>
/// is the channel that failed before it.
/// </summary>
public sealed record ThreadDeliveryDto(string Status, string? Channel, string? ErrorCode, string? FallbackFrom);

/// <summary>
/// One message of a thread. <c>direction</c> is <c>inbound</c> (from the customer) or
/// <c>outbound</c> (a staff reply); <c>kind</c> is <c>text</c>, <c>image</c>, <c>video</c>,
/// <c>audio</c>, <c>file</c>, <c>location</c>, <c>sticker</c> or <c>other</c>, and only text,
/// a file's name and a location's address are kept as words.
/// </summary>
public sealed record ThreadMessageDto(
    string Id,
    string ThreadId,
    string Direction,
    string? Channel,
    string Kind,
    string? Text,
    DateTimeOffset SentAt,
    string? SentByStaffId,
    string? SentByName,
    ThreadDeliveryDto? Delivery)
{
    public static ThreadMessageDto From(ThreadMessage message, string? sentByName) => new(
        message.Id,
        message.ThreadId,
        WireEnum.Of(message.Direction),
        message.Channel is { } channel ? WireEnum.Of(channel) : null,
        WireEnum.Of(message.Kind),
        message.Text,
        message.SentAt,
        message.SentByStaffId,
        sentByName,
        message.DeliveryStatus is { } status
            ? new ThreadDeliveryDto(
                WireEnum.Of(status),
                message.Channel is { } used ? WireEnum.Of(used) : null,
                message.DeliveryErrorCode,
                message.FallbackFrom is { } from ? WireEnum.Of(from) : null)
            : null);

    public static ThreadMessageDto From(ThreadMessageRow row) => From(row.Message, row.SentByName);
}

/// <summary>
/// One entry of <c>GET /staff/threads</c>: one customer's conversation across every channel.
/// <para>
/// <c>status</c> is <c>awaiting_reply</c> when the customer wrote after the clinic last answered,
/// otherwise <c>replied</c>. <c>unread_count</c> counts the customer's messages since the last
/// reply. <c>reply_channel</c> is the channel a reply tries first: the one the customer used last.
/// <c>messages</c> holds the newest messages, oldest first, as many as <c>?messages=</c> asked for;
/// <c>has_more_messages</c> says older ones exist.
/// </para>
/// </summary>
public sealed record ThreadDto(
    string Id,
    string CustomerId,
    string? CustomerName,
    string? BranchId,
    string Status,
    int UnreadCount,
    int MessageCount,
    string? ReplyChannel,
    DateTimeOffset LastMessageAt,
    DateTimeOffset? LastInboundAt,
    DateTimeOffset? LastReplyAt,
    ThreadMessageDto? LastMessage,
    IReadOnlyList<ThreadMessageDto> Messages,
    bool HasMoreMessages,
    uint Version)
{
    public const string AwaitingReply = "awaiting_reply";
    public const string Replied = "replied";

    public static ThreadDto From(ThreadRow row, IReadOnlyList<ThreadMessageRow> messages)
    {
        var thread = row.Thread;

        return new(
            thread.Id,
            thread.CustomerId,
            row.CustomerName,
            thread.BranchId,
            thread.AwaitingReply ? AwaitingReply : Replied,
            row.UnreadCount,
            row.MessageCount,
            thread.LastInboundChannel is { } channel ? WireEnum.Of(channel) : null,
            thread.LastMessageAt,
            thread.LastInboundAt,
            thread.LastReplyAt,
            row.LastMessage is { } last ? ThreadMessageDto.From(last, null) : null,
            [.. messages.OrderBy(item => item.Message.SentAt).ThenBy(item => item.Message.Id, StringComparer.Ordinal).Select(ThreadMessageDto.From)],
            row.MessageCount > messages.Count,
            thread.Version);
    }
}
