using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>
/// <c>docs/contract-extensions.md § 22</c> in the domain: a webhook that can be edited, switched
/// off, given a new secret and deleted without losing its log; a delivery that can be stopped and
/// sent again once; and a key whose state reads at a glance.
/// </summary>
public class IntegrationManagementTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_webhook_can_be_edited_and_switched_off()
    {
        var subscription = Subscription();

        subscription.ChangeUrl(" https://hooks.example.com/v2 ");
        subscription.ChangeEvents(["pet.created", "customer.created", "pet.created"]);
        subscription.ChangeDescription("  ");
        subscription.SetActive(false);

        Assert.Equal("https://hooks.example.com/v2", subscription.Url);
        Assert.Equal(["customer.created", "pet.created"], subscription.Events);
        Assert.Null(subscription.Description);
        Assert.False(subscription.Receives(WebhookEventTypes.CustomerCreated));
        Assert.False(subscription.IsDeleted);
        Assert.Throws<DomainException>(() => subscription.ChangeEvents([]));
    }

    [Fact]
    public void A_deleted_webhook_loses_its_secret_and_query_string_and_cannot_change()
    {
        var subscription = WebhookSubscription.Create(
            "https://hooks.example.com/x?receiver_token=abc", ["invoice.issued"], "บัญชี", "cipher", "stf_1");

        subscription.Delete();

        Assert.True(subscription.IsDeleted);
        Assert.False(subscription.Active);
        Assert.Equal(string.Empty, subscription.SecretCiphertext);
        Assert.Equal("https://hooks.example.com/x", subscription.Url);
        Assert.Throws<DomainException>(() => subscription.SetActive(true));
        Assert.Throws<DomainException>(() => subscription.RotateSecret("new"));
        Assert.Throws<DomainException>(() => subscription.Delete());
    }

    [Fact]
    public void Rotating_replaces_the_secret_and_refuses_an_empty_one()
    {
        var subscription = Subscription();

        subscription.RotateSecret("cipher-2");

        Assert.Equal("cipher-2", subscription.SecretCiphertext);
        Assert.Throws<DomainException>(() => subscription.RotateSecret(string.Empty));
    }

    [Fact]
    public void A_halted_delivery_counts_no_attempt_and_resumes_its_retries_when_sent_again()
    {
        var delivery = WebhookDelivery.Queue("whk_1", Event(), Start);
        delivery.RecordFailure(Start, 503, "http_503", null, 5);

        delivery.Halt(Start.AddMinutes(1), WebhookDelivery.HaltedDisabled);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(WebhookDelivery.HaltedDisabled, delivery.LastError);
        Assert.Null(delivery.NextAttemptAt);

        var again = Start.AddMinutes(2);
        delivery.Redeliver(again);

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(again, delivery.NextAttemptAt);
        Assert.Null(delivery.CompletedAt);

        delivery.RecordFailure(again, 503, "http_503", null, 5);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(2, delivery.Attempts);
    }

    [Fact]
    public void An_exhausted_delivery_sent_again_gets_exactly_one_more_attempt()
    {
        var delivery = WebhookDelivery.Queue("whk_1", Event(), Start);
        for (var attempt = 0; attempt < WebhookRetrySchedule.MaxAttempts; attempt++)
        {
            delivery.RecordFailure(Start, 500, "http_500", null, 1);
        }

        delivery.Redeliver(Start);
        delivery.RecordFailure(Start, 500, "http_500", null, 1);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(WebhookRetrySchedule.MaxAttempts + 1, delivery.Attempts);
    }

    [Fact]
    public void Only_a_failed_delivery_can_be_sent_again_and_only_a_pending_one_halted()
    {
        var pending = WebhookDelivery.Queue("whk_1", Event(), Start);
        var succeeded = WebhookDelivery.Queue("whk_1", Event(), Start);
        succeeded.RecordSuccess(Start, 200, null, 1);

        Assert.Throws<DomainException>(() => pending.Redeliver(Start));
        Assert.Throws<DomainException>(() => succeeded.Redeliver(Start));
        Assert.Throws<InvalidOperationException>(() => succeeded.Halt(Start, WebhookDelivery.HaltedDeleted));
    }

    [Fact]
    public void A_keys_state_reads_active_until_it_expires()
    {
        var (key, _) = ApiKey.Issue("x", ["customer:read"], [], Start.AddDays(30), Start, "usr_1", "stf_1");

        Assert.Equal("active", key.StateAt(Start));
        Assert.Equal("expired", key.StateAt(Start.AddDays(31)));
    }

    private static WebhookSubscription Subscription() =>
        WebhookSubscription.Create("https://hooks.example.com/x", ["invoice.issued"], "บัญชี", "cipher", "stf_1");

    private static WebhookEvent Event() =>
        WebhookEvent.Create("evt_1", WebhookEventTypes.InvoiceIssued, "{}", Start);
}
