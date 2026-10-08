using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>The LINE channel's settings, and the data a chat button carries.</summary>
public class LineChannelSettingsTests
{
    [Fact]
    public void A_new_clinic_has_LINE_on_every_type_on_and_the_default_quota()
    {
        var settings = LineChannelSettings.CreateDefault();

        Assert.True(settings.Enabled);
        Assert.True(settings.IsTemplateEnabled("queue.called"));
        Assert.Equal(LineChannelSettings.DefaultBroadcastMonthlyLimit, settings.BroadcastMonthlyLimit);
        Assert.Null(settings.ChannelAccessToken);
    }

    [Fact]
    public void An_absent_value_is_left_alone_and_an_empty_one_clears_a_credential()
    {
        var settings = LineChannelSettings.CreateDefault();
        settings.Update(channelId: "1650000000", channelSecret: "secret", channelAccessToken: "token");

        settings.Update(enabled: false);
        Assert.Equal("secret", settings.ChannelSecret);
        Assert.False(settings.Enabled);

        settings.Update(channelAccessToken: "");
        Assert.Null(settings.ChannelAccessToken);
        Assert.Equal("1650000000", settings.ChannelId);
    }

    [Fact]
    public void A_notification_type_is_switched_off_by_listing_it()
    {
        var settings = LineChannelSettings.CreateDefault();
        settings.Update(disabledTemplates: ["receipt.issued", "receipt.issued"]);

        Assert.False(settings.IsTemplateEnabled("receipt.issued"));
        Assert.True(settings.IsTemplateEnabled("queue.called"));
        Assert.Single(settings.DisabledTemplates);
    }

    [Theory]
    [InlineData("12ab")]
    [InlineData("123456789012345678901")]
    public void A_channel_id_is_digits_only(string channelId) =>
        Assert.Throws<DomainException>(() => LineChannelSettings.CreateDefault().Update(channelId: channelId));

    [Fact]
    public void A_credential_with_spaces_or_a_negative_quota_is_refused()
    {
        Assert.Throws<DomainException>(() => LineChannelSettings.CreateDefault().Update(channelSecret: "two words"));
        Assert.Throws<DomainException>(() => LineChannelSettings.CreateDefault().Update(broadcastMonthlyLimit: -1));
    }

    [Fact]
    public void Button_data_round_trips_and_escapes_its_values()
    {
        var data = LinePostbackData.Build(
            LinePostbackActions.CancelAppointment, (LinePostbackData.AppointmentId, "apt_1&x=2"), (LinePostbackData.Confirmed, "1"));

        var parsed = LinePostbackData.Parse(data)!;

        Assert.Equal(LinePostbackActions.CancelAppointment, parsed.Action);
        Assert.Equal("apt_1&x=2", parsed[LinePostbackData.AppointmentId]);
        Assert.Equal("1", parsed[LinePostbackData.Confirmed]);
    }

    [Theory]
    [InlineData("action=delete_everything&appointment_id=apt_1")]
    [InlineData("appointment_id=apt_1")]
    [InlineData("")]
    [InlineData(null)]
    public void Data_naming_no_known_action_is_refused(string? data) => Assert.Null(LinePostbackData.Parse(data));
}
