using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Messaging;

/// <summary>
/// One customer's conversation with the clinic: "กล่องข้อความรวมทุกช่องทางต่อลูกค้าหนึ่งคน".
/// <para>
/// <b>One thread per customer, whatever the channel.</b> A message that arrives on LINE today and
/// one that arrives by another channel tomorrow land in the same thread, and a reply goes back on
/// the channel the customer used last ("ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด").
/// </para>
/// <para>
/// <b>Started by the customer.</b> The contract has no endpoint for staff to open a thread, so a
/// thread is created by the first message a customer sends. Staff who want to write first use
/// <c>POST /staff/notifications/send</c>.
/// </para>
/// <para>
/// The summary columns (<see cref="LastMessageAt"/>, <see cref="LastInboundAt"/>,
/// <see cref="LastReplyAt"/>) are kept on the row so the inbox can be listed and ordered without
/// reading every message. A thread is <see cref="AwaitingReply"/> when the customer wrote after the
/// clinic last answered.
/// </para>
/// </summary>
public sealed class MessageThread : Entity
{
    private MessageThread()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>
    /// The branch the thread is filed under, for branch-bound staff: where the customer last booked.
    /// Null when they never booked, which makes the thread visible to every branch.
    /// </summary>
    public string? BranchId { get; private set; }

    /// <summary>When the newest message, in either direction, was written.</summary>
    public DateTimeOffset LastMessageAt { get; private set; }

    /// <summary>When the customer last wrote.</summary>
    public DateTimeOffset? LastInboundAt { get; private set; }

    /// <summary>
    /// The channel the customer last wrote on: the one a reply tries first. <c>portal</c> has no
    /// channel to try first, so such a reply takes the usual order (and the web inbox, as always).
    /// </summary>
    public MessageChannel? LastInboundChannel { get; private set; }

    /// <summary>When staff last replied.</summary>
    public DateTimeOffset? LastReplyAt { get; private set; }

    /// <summary>The customer wrote after the clinic last answered.</summary>
    public bool AwaitingReply => LastInboundAt is { } inbound && (LastReplyAt is null || inbound > LastReplyAt);

    /// <summary>A new, empty thread for a customer. The first message is added with <see cref="Receive"/>.</summary>
    public static MessageThread Open(string customerId, string? branchId, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainException("invalid_thread", "ต้องระบุลูกค้า");
        }

        return new MessageThread
        {
            Id = EntityId.New(EntityId.MessageThread),
            CustomerId = customerId,
            BranchId = branchId,
            LastMessageAt = at,
        };
    }

    /// <summary>
    /// A message from the customer. The thread is refiled under the branch they last booked at, when
    /// that is known, since that is the branch most likely to answer.
    /// </summary>
    public ThreadMessage Receive(
        MessageChannel channel,
        MessageKind kind,
        string? text,
        string? externalId,
        string? branchId,
        DateTimeOffset at)
    {
        var message = ThreadMessage.Inbound(Id, channel, kind, text, externalId, at);

        LastInboundAt = Later(LastInboundAt, at);
        LastInboundChannel = LastInboundAt == at ? channel : LastInboundChannel;
        LastMessageAt = Later(LastMessageAt, at)!.Value;

        if (branchId is not null)
        {
            BranchId = branchId;
        }

        return message;
    }

    /// <summary>A reply written by staff. Sending it is the caller's job, after the write commits.</summary>
    public ThreadMessage Reply(string staffId, string text, DateTimeOffset at)
    {
        var message = ThreadMessage.Outbound(Id, staffId, text, at);

        LastReplyAt = Later(LastReplyAt, at);
        LastMessageAt = Later(LastMessageAt, at)!.Value;

        return message;
    }

    /// <summary>
    /// Takes over another thread's summary when two customer records are merged and the other
    /// thread's messages have been moved into this one.
    /// </summary>
    public void Absorb(MessageThread other)
    {
        if (other.LastInboundAt is { } inbound && (LastInboundAt is null || inbound > LastInboundAt))
        {
            LastInboundAt = inbound;
            LastInboundChannel = other.LastInboundChannel;
        }

        LastReplyAt = Later(LastReplyAt, other.LastReplyAt);
        LastMessageAt = Later(LastMessageAt, other.LastMessageAt)!.Value;
        BranchId ??= other.BranchId;
    }

    private static DateTimeOffset? Later(DateTimeOffset? current, DateTimeOffset? candidate) =>
        current is null || (candidate is not null && candidate > current) ? candidate : current;
}
