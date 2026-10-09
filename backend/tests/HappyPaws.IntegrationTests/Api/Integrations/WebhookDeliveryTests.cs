using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Integrations.DispatchWebhooks;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// The webhook job, end to end: a business change queues an event, the job fans it out and sends
/// it, signed, to the fake receiver; and every way a send can go wrong is recorded and retried by
/// the contract's schedule, or refused before it leaves.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class WebhookDeliveryTests(IntegrationsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_delivery_is_signed_with_the_secret_and_carries_the_event()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var secret = webhook.GetProperty("secret").GetString()!;

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        var customerId = await IntegrationsScenario.CreateCustomerAsync(counter, Token, name: "คุณลับเฉพาะ ไม่ควรออก");

        await fixture.DrainAsync(Token);

        var received = Assert.Single(fixture.Receiver.To(host));
        Assert.Equal("/hooks/clinic", received.Path);
        Assert.Equal("customer.created", received.Headers["X-Webhook-Event"]);
        Assert.StartsWith("evt_", received.Headers["X-Webhook-Id"], StringComparison.Ordinal);
        Assert.StartsWith("application/json", received.Headers["Content-Type"], StringComparison.Ordinal);

        var timestamp = long.Parse(received.Headers["X-Webhook-Timestamp"], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(WebhookSignature.Verify(secret, timestamp, received.Body, received.Headers["X-Signature"]));
        Assert.True(Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) < 120);

        var body = JsonDocument.Parse(received.Body).RootElement;
        Assert.Equal("customer.created", body.GetProperty("type").GetString());
        Assert.Equal(received.Headers["X-Webhook-Id"], body.GetProperty("id").GetString());
        Assert.Equal(customerId, body.GetProperty("data").GetProperty("customer_id").GetString());

        // A thin event: the receiver gets the id, not the person.
        Assert.DoesNotContain("ลับเฉพาะ", received.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("phone", received.Body, StringComparison.Ordinal);

        var deliveries = await fixture.Integrator(clinic).GetFromJsonAsync<JsonElement>(
            $"/staff/webhooks/{webhook.GetProperty("id").GetString()}/deliveries", Token);
        var row = Assert.Single(deliveries.GetProperty("data").EnumerateArray());
        Assert.Equal("succeeded", row.GetProperty("status").GetString());
        Assert.Equal(1, row.GetProperty("attempts").GetInt32());
        Assert.Equal(204, row.GetProperty("last_status_code").GetInt32());
    }

    [Fact]
    public async Task Only_subscriptions_that_want_the_type_and_existed_at_the_time_get_it()
    {
        var (clinic, wanted, _) = await SubscribeAsync(["customer.created"]);
        var (_, unwanted, _) = await SubscribeAsync(["invoice.issued"]);

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);

        var (_, late, _) = await SubscribeAsync(["customer.created"]);

        await fixture.DrainAsync(Token);

        Assert.NotEmpty(fixture.Receiver.To(wanted));
        Assert.Empty(fixture.Receiver.To(unwanted));
        Assert.Empty(fixture.Receiver.To(late));
    }

    [Fact]
    public async Task A_failing_receiver_is_retried_on_the_schedule_and_given_up_on_after_the_sixth_attempt()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("down for maintenance") });

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);

        var subscriptionId = webhook.GetProperty("id").GetString()!;
        await fixture.DrainAsync(Token);

        for (var attempt = 1; attempt <= WebhookRetrySchedule.MaxAttempts; attempt++)
        {
            var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, subscriptionId, Token));

            Assert.Equal(attempt, delivery.Attempts);
            Assert.Equal(503, delivery.LastStatusCode);
            Assert.Equal("http_503", delivery.LastError);
            Assert.Equal("down for maintenance", delivery.LastResponseBody);

            if (attempt < WebhookRetrySchedule.MaxAttempts)
            {
                Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
                var wait = delivery.NextAttemptAt!.Value - delivery.LastAttemptAt!.Value;
                Assert.Equal(WebhookRetrySchedule.DelayAfter(attempt)!.Value, wait);

                // Not yet due: the job leaves it alone.
                await fixture.DrainAsync(Token);
                Assert.Equal(attempt, fixture.Receiver.To(host).Count);

                await IntegrationsScenario.MakeDueAsync(fixture, delivery.Id, Token);
                await fixture.DrainAsync(Token);
            }
            else
            {
                Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
                Assert.Null(delivery.NextAttemptAt);
            }
        }

        await fixture.DrainAsync(Token);
        Assert.Equal(WebhookRetrySchedule.MaxAttempts, fixture.Receiver.To(host).Count);
    }

    [Fact]
    public async Task A_receiver_that_recovers_gets_the_same_event_with_the_same_id()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var failing = true;
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(failing ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        failing = false;
        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        await IntegrationsScenario.MakeDueAsync(fixture, delivery.Id, Token);
        await fixture.DrainAsync(Token);

        var attempts = fixture.Receiver.To(host);
        Assert.Equal(2, attempts.Count);
        Assert.Equal(attempts[0].Headers["X-Webhook-Id"], attempts[1].Headers["X-Webhook-Id"]);
        Assert.Equal(attempts[0].Body, attempts[1].Body);

        var done = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        Assert.Equal(WebhookDeliveryStatus.Succeeded, done.Status);
        Assert.Equal(2, done.Attempts);
    }

    [Fact]
    public async Task A_redirect_is_recorded_as_a_failure_and_not_followed()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        fixture.Receiver.Answer(host, () =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("http://169.254.169.254/latest/meta-data");
            return redirect;
        });

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        Assert.Single(fixture.Receiver.To(host));
        Assert.Empty(fixture.Receiver.To("169.254.169.254"));

        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        Assert.Equal("redirect_not_followed", delivery.LastError);
        Assert.Equal(302, delivery.LastStatusCode);
    }

    [Theory]
    [InlineData("10.1.2.3")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("192.168.0.10")]
    [InlineData("::1")]
    [InlineData("fd00:ec2::254")]
    [InlineData("::ffff:10.0.0.1")]
    public async Task A_name_that_resolves_to_a_private_address_is_never_contacted(string address)
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);

        // Public when the webhook was made; private by the time it is sent (DNS rebinding).
        fixture.Resolver.Map(host, address);

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        Assert.Empty(fixture.Receiver.To(host));

        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        Assert.Equal("address_blocked", delivery.LastError);
        Assert.Null(delivery.LastStatusCode);
    }

    [Fact]
    public async Task A_name_with_one_public_and_one_private_address_is_refused()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        fixture.Resolver.Map(host, "93.184.216.34", "10.0.0.8");

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        Assert.Empty(fixture.Receiver.To(host));
        Assert.Equal(
            "address_blocked",
            Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token)).LastError);
    }

    [Fact]
    public async Task A_slow_receiver_times_out()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        fixture.Receiver.Delay(host, TimeSpan.FromSeconds(5));

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        Assert.Equal("timeout", delivery.LastError);
        Assert.True(delivery.LastDurationMs < 4_000);
    }

    [Fact]
    public async Task Only_the_start_of_a_long_answer_is_kept()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(new string('x', 100_000)) });

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, webhook.GetProperty("id").GetString()!, Token));
        Assert.Equal(WebhookDelivery.ResponseBodyMaxLength, delivery.LastResponseBody!.Length);
    }

    [Fact]
    public async Task Two_workers_never_take_the_same_delivery()
    {
        await fixture.DrainAsync(Token);

        var (clinic, host, _) = await SubscribeAsync(["customer.created"]);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<DispatchWebhooksHandler>().FanOutAsync(Token);
        }

        using var first = fixture.CreateScope();
        using var second = fixture.CreateScope();
        var one = first.ServiceProvider.GetRequiredService<DispatchWebhooksHandler>();
        var two = second.ServiceProvider.GetRequiredService<DispatchWebhooksHandler>();

        // Both workers take everything that is due, racing for each row.
        var taken = new List<string>();

        while (true)
        {
            var claims = await Task.WhenAll(one.ClaimAsync(Token), two.ClaimAsync(Token));

            if (claims.All(claim => claim is null))
            {
                break;
            }

            taken.AddRange(claims.Where(claim => claim is not null).Select(claim => claim!.Id));
        }

        Assert.NotEmpty(taken);
        Assert.Equal(taken.Count, taken.Distinct().Count());

        // A leased delivery is not due again until its lease runs out.
        await fixture.DrainAsync(Token);
        Assert.Empty(fixture.Receiver.To(host));
    }

    private async Task<(SeededClinic Clinic, string Host, JsonElement Webhook)> SubscribeAsync(IReadOnlyList<string> events)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var host = IntegrationsScenario.Host();

        using var integrator = fixture.Integrator(clinic);
        var webhook = await IntegrationsScenario.CreateWebhookAsync(integrator, host, events, Token);

        return (clinic, host, webhook);
    }
}
