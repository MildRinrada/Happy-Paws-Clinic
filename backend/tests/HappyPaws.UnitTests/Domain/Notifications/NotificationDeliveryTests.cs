using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.UnitTests.Domain.Notifications;

/// <summary>The delivery log's rows, how addresses appear in them, and the customer's LINE address.</summary>
public class NotificationDeliveryTests
{
    [Fact]
    public void A_row_names_its_attempt_and_has_its_own_id()
    {
        var row = NotificationDelivery.Record(
            "cus_1", "queue.called", DeliveryStatus.Sent, DeliverySource.System, DeliveryChannel.Line, "U***abcd");

        Assert.StartsWith("ndl_", row.Id);
        Assert.Equal(DeliveryChannel.Line, row.Channel);
        Assert.Null(row.ErrorCode);
    }

    [Fact]
    public void A_sent_row_must_say_which_channel_carried_it()
    {
        Assert.Throws<DomainException>(() =>
            NotificationDelivery.Record("cus_1", "queue.called", DeliveryStatus.Sent, DeliverySource.System));
    }

    [Fact]
    public void A_skipped_row_needs_no_channel()
    {
        var row = NotificationDelivery.Record(
            "cus_1", "queue.called", DeliveryStatus.Skipped, DeliverySource.Manual, errorCode: "no_consent");

        Assert.Null(row.Channel);
        Assert.Equal("no_consent", row.ErrorCode);
    }

    [Theory]
    [InlineData("081-234-5678", "***5678")]
    [InlineData("0812345678", "***5678")]
    [InlineData("12", "***12")]
    public void A_phone_number_shows_only_its_last_four_digits(string phone, string masked) =>
        Assert.Equal(masked, RecipientMask.Phone(phone));

    [Fact]
    public void An_email_and_a_LINE_id_show_too_little_to_copy()
    {
        Assert.Equal("s***@mail.com", RecipientMask.Email("somchai@mail.com"));
        Assert.Equal("U***cdef", RecipientMask.Line("U1234567890abcdef1234567890abcdef"));
        Assert.Equal("***", RecipientMask.Email("broken"));
    }

    [Fact]
    public void A_customer_who_blocks_the_account_stops_receiving_until_they_follow_again()
    {
        var customer = Customer.Create("คุณลูกค้า", "0812345678");
        Assert.False(customer.CanReceiveLine);

        customer.SetLineAddress("U1234567890abcdef1234567890abcdef");
        Assert.True(customer.CanReceiveLine);

        customer.MarkLineUnfollowed(DateTimeOffset.UtcNow);
        Assert.False(customer.CanReceiveLine);

        customer.MarkLineFollowed();
        Assert.True(customer.CanReceiveLine);

        customer.SetLineAddress(null);
        Assert.False(customer.CanReceiveLine);
    }
}
