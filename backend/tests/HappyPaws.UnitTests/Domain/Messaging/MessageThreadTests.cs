using HappyPaws.Domain.Common;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.UnitTests.Domain.Messaging;

/// <summary>A customer's thread: when it awaits a reply, which channel a reply tries first, and what a merge keeps.</summary>
public class MessageThreadTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 9, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_message_from_the_customer_makes_the_thread_await_a_reply_until_staff_answer()
    {
        var thread = MessageThread.Open("cus_1", null, Noon);
        Assert.False(thread.AwaitingReply);

        var inbound = thread.Receive(DeliveryChannel.Line, MessageKind.Text, "  สวัสดีค่ะ  ", "m1", "brn_1", Noon);

        Assert.True(thread.AwaitingReply);
        Assert.Equal(DeliveryChannel.Line, thread.LastInboundChannel);
        Assert.Equal("brn_1", thread.BranchId);
        Assert.Equal(("สวัสดีค่ะ", MessageDirection.Inbound, thread.Id), (inbound.Text, inbound.Direction, inbound.ThreadId));

        var reply = thread.Reply("stf_1", "ค่ะ", Noon.AddMinutes(5));

        Assert.False(thread.AwaitingReply);
        Assert.Equal(Noon.AddMinutes(5), thread.LastMessageAt);
        Assert.Equal((MessageDirection.Outbound, "stf_1"), (reply.Direction, reply.SentByStaffId));
        Assert.Null(reply.Channel);

        thread.Receive(DeliveryChannel.Line, MessageKind.Sticker, null, "m2", null, Noon.AddMinutes(9));
        Assert.True(thread.AwaitingReply);
        Assert.Equal("brn_1", thread.BranchId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_reply_is_refused(string text)
    {
        var thread = MessageThread.Open("cus_1", null, Noon);

        Assert.Throws<DomainException>(() => thread.Reply("stf_1", text, Noon));
    }

    [Fact]
    public void A_reply_longer_than_SMS_can_reasonably_carry_is_refused()
    {
        var thread = MessageThread.Open("cus_1", null, Noon);

        Assert.Throws<DomainException>(() => thread.Reply("stf_1", new string('ก', ThreadMessage.ReplyTextMax + 1), Noon));
        Assert.Equal(ThreadMessage.ReplyTextMax, thread.Reply("stf_1", new string('ก', ThreadMessage.ReplyTextMax), Noon).Text!.Length);
    }

    [Fact]
    public void A_delivery_outcome_belongs_to_replies_only()
    {
        var thread = MessageThread.Open("cus_1", null, Noon);
        var inbound = thread.Receive(DeliveryChannel.Line, MessageKind.Text, "hi", null, null, Noon);
        var reply = thread.Reply("stf_1", "hello", Noon);

        reply.RecordDelivery(DeliveryStatus.Sent, DeliveryChannel.Sms, null, DeliveryChannel.Line);

        Assert.Equal((DeliveryStatus.Sent, DeliveryChannel.Sms, DeliveryChannel.Line), (reply.DeliveryStatus!.Value, reply.Channel!.Value, reply.FallbackFrom!.Value));
        Assert.Throws<DomainException>(() => inbound.RecordDelivery(DeliveryStatus.Sent, DeliveryChannel.Line, null, null));
    }

    [Fact]
    public void Absorbing_a_merged_records_thread_keeps_the_latest_of_each_and_the_channel_of_the_latest_message()
    {
        var surviving = MessageThread.Open("cus_1", null, Noon);
        surviving.Receive(DeliveryChannel.Line, MessageKind.Text, "a", null, null, Noon);
        surviving.Reply("stf_1", "b", Noon.AddMinutes(1));

        var duplicate = MessageThread.Open("cus_2", "brn_2", Noon);
        duplicate.Receive(DeliveryChannel.Sms, MessageKind.Text, "c", null, null, Noon.AddMinutes(2));

        surviving.Absorb(duplicate);

        Assert.True(surviving.AwaitingReply);
        Assert.Equal(DeliveryChannel.Sms, surviving.LastInboundChannel);
        Assert.Equal(Noon.AddMinutes(2), surviving.LastMessageAt);
        Assert.Equal("brn_2", surviving.BranchId);
    }
}
