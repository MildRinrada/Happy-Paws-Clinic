using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.UnitTests.Domain.Automation;

/// <summary>The "already sent" record and how a reminder's outcome is read from the delivery log.</summary>
public class AutomationDispatchTests
{
    private static NotificationDelivery Row(DeliveryStatus status, string? error = null, DeliveryChannel? channel = null) =>
        NotificationDelivery.Record("cus_1", "vaccine.due", status, DeliverySource.Automation, channel, errorCode: error);

    [Fact]
    public void A_claim_starts_claimed_with_its_own_id()
    {
        var now = DateTimeOffset.UtcNow;
        var claim = AutomationDispatch.Claim("atm_1", AutomationSubjectKind.Vaccination, "vac_1", "2026-10-20", "cus_1", "pet_1", null, now, now, "host:1");

        Assert.StartsWith("ard_", claim.Id);
        Assert.Equal(DispatchStatus.Claimed, claim.Status);
        Assert.Equal(now, claim.CreatedAt);
    }

    [Fact]
    public void A_claim_needs_a_subject_and_a_due_key()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() => AutomationDispatch.Claim("atm_1", AutomationSubjectKind.Appointment, " ", "k", "cus_1", null, null, now, now, "w"));
        Assert.Throws<DomainException>(() => AutomationDispatch.Claim("atm_1", AutomationSubjectKind.Appointment, "apt_1", new string('k', 41), "cus_1", null, null, now, now, "w"));
    }

    [Fact]
    public void Any_channel_that_took_it_makes_it_sent()
    {
        var outcome = AutomationDispatch.OutcomeOf([Row(DeliveryStatus.Failed, "provider_error", DeliveryChannel.Line), Row(DeliveryStatus.Sent, channel: DeliveryChannel.Sms)]);

        Assert.Equal((DispatchStatus.Sent, null), outcome);
    }

    [Fact]
    public void Skipped_everywhere_is_skipped_with_the_first_reason()
    {
        var outcome = AutomationDispatch.OutcomeOf([Row(DeliveryStatus.Skipped, "no_consent")]);

        Assert.Equal((DispatchStatus.Skipped, "no_consent"), outcome);
    }

    [Fact]
    public void Refused_everywhere_is_failed_with_the_last_refusal()
    {
        var outcome = AutomationDispatch.OutcomeOf([Row(DeliveryStatus.Skipped, "no_address"), Row(DeliveryStatus.Failed, "provider_quota", DeliveryChannel.Sms)]);

        Assert.Equal((DispatchStatus.Failed, "provider_quota"), outcome);
    }

    [Fact]
    public void A_channel_that_keeps_no_log_accepted_it()
    {
        Assert.Equal((DispatchStatus.Sent, null), AutomationDispatch.OutcomeOf([]));
    }
}
