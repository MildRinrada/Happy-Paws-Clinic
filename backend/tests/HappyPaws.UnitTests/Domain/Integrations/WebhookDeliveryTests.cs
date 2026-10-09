using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>
/// The contract's "retry: 5 ครั้ง แบบ exponential ภายใน 24 ชม.": one attempt and five retries,
/// each delay five times the last, all inside a day; then the delivery gives up.
/// </summary>
public class WebhookDeliveryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_schedule_is_five_exponential_retries_inside_24_hours()
    {
        var delays = Enumerable.Range(1, WebhookRetrySchedule.MaxRetries)
            .Select(attempt => WebhookRetrySchedule.DelayAfter(attempt)!.Value)
            .ToList();

        Assert.Equal(5, delays.Count);
        Assert.Equal(TimeSpan.FromMinutes(1), delays[0]);

        for (var i = 1; i < delays.Count; i++)
        {
            Assert.Equal(delays[i - 1] * 5, delays[i]);
        }

        Assert.True(delays.Aggregate(TimeSpan.Zero, (sum, delay) => sum + delay) < TimeSpan.FromHours(24));
        Assert.Null(WebhookRetrySchedule.DelayAfter(WebhookRetrySchedule.MaxAttempts));
    }

    [Fact]
    public void A_failure_schedules_the_next_attempt_and_the_sixth_gives_up()
    {
        var delivery = WebhookDelivery.Queue("whk_1", Event(), Start);
        var at = Start;

        for (var attempt = 1; attempt < WebhookRetrySchedule.MaxAttempts; attempt++)
        {
            delivery.RecordFailure(at, 503, "http_503", "busy", 12);

            Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
            Assert.Equal(attempt, delivery.Attempts);
            Assert.Equal(at + WebhookRetrySchedule.DelayAfter(attempt)!.Value, delivery.NextAttemptAt);

            at = delivery.NextAttemptAt!.Value;
        }

        delivery.RecordFailure(at, 503, "http_503", "busy", 12);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(WebhookRetrySchedule.MaxAttempts, delivery.Attempts);
        Assert.Null(delivery.NextAttemptAt);
        Assert.Equal(at, delivery.CompletedAt);
        Assert.True(at - Start < TimeSpan.FromHours(24));
    }

    [Fact]
    public void A_success_ends_the_delivery()
    {
        var delivery = WebhookDelivery.Queue("whk_1", Event(), Start);

        delivery.RecordSuccess(Start, 200, "ok", 30);

        Assert.Equal(WebhookDeliveryStatus.Succeeded, delivery.Status);
        Assert.Null(delivery.NextAttemptAt);
        Assert.Throws<InvalidOperationException>(() => delivery.RecordFailure(Start, 500, "http_500", null, 1));
    }

    [Fact]
    public void The_receivers_answer_is_cut_short()
    {
        var delivery = WebhookDelivery.Queue("whk_1", Event(), Start);

        delivery.RecordFailure(Start, 500, new string('e', 1_000), new string('x', 50_000), 5);

        Assert.Equal(WebhookDelivery.ResponseBodyMaxLength, delivery.LastResponseBody!.Length);
        Assert.Equal(WebhookDelivery.ErrorMaxLength, delivery.LastError!.Length);
    }

    [Fact]
    public void A_subscription_wants_only_the_events_it_lists()
    {
        var subscription = WebhookSubscription.Create(
            "https://hooks.example.com/x", ["invoice.issued", "invoice.issued"], null, "cipher", "stf_1");

        Assert.Equal(["invoice.issued"], subscription.Events);
        Assert.True(subscription.Receives(WebhookEventTypes.InvoiceIssued));
        Assert.False(subscription.Receives(WebhookEventTypes.CustomerCreated));
    }

    [Fact]
    public void An_unknown_event_type_is_refused()
    {
        Assert.Throws<HappyPaws.Domain.Common.DomainException>(() =>
            WebhookSubscription.Create("https://hooks.example.com/x", ["everything"], null, "cipher", "stf_1"));
    }

    private static WebhookEvent Event() =>
        WebhookEvent.Create("evt_1", WebhookEventTypes.InvoiceIssued, "{}", Start);
}
