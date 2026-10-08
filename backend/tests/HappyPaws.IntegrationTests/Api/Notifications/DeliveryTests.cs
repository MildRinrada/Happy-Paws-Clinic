using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Notifications;

/// <summary>
/// Messages really going out: LINE first, SMS when LINE cannot or fails, email when asked for, and a
/// row in the delivery log for every attempt. Only the providers at the edge are fakes.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class DeliveryTests(DeliveryFixture fixture) : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        fixture.Transports.Reset();
        fixture.Line.Status = HttpStatusCode.OK;
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private Task<HttpResponseMessage> SendAsync(object body) =>
        fixture.Notifier(Scopes.Notify.Send).PostAsJsonAsync("/staff/notifications/send", body, Token);

    private static object Reminder(string customerId, string channel = "auto") => new
    {
        template = "appointment.reminder",
        customer_ids = new[] { customerId },
        channel,
        variables = new { pet_name = "ลูน่า", date = "12 ต.ค.", time = "10:30", vet_name = "หมอเอ" },
    };

    private async Task CallPatientAsync(SeededClinic clinic)
    {
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 3, Token);
        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.CheckIn, Scopes.Queue.Call], [clinic.BranchId]));
        var ticketId = await CheckInAsync(staff, appointmentId, Token);
        (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/call", new { }, Token)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_queue_call_is_pushed_to_the_owners_LINE_with_a_button_to_see_the_queue()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);

        await CallPatientAsync(clinic);

        var push = Assert.Single(fixture.Line.PushesTo(lineUserId));
        Assert.Equal($"Bearer {NotificationScenario.AccessToken}", push.Authorization);
        Assert.NotNull(push.RetryKey);
        Assert.StartsWith("ถึงคิว", push.Text);
        Assert.Contains(push.Buttons, data => data.StartsWith("action=view_queue", StringComparison.Ordinal));

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal((DeliveryStatus.Sent, DeliveryChannel.Line), (row.Status, row.Channel));
        Assert.Equal($"U***{lineUserId[^4..]}", row.Recipient);
        Assert.Equal(DeliverySource.System, row.Source);
        Assert.Equal(clinic.BranchId, row.BranchId);
    }

    [Fact]
    public async Task A_receipt_reaches_LINE_with_the_amount_and_a_button_to_open_the_bill()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        (await BillingScenario.PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();
        (await cashier.PostAsJsonAsync($"/staff/invoices/{invoiceId}/receipt", new { }, Token)).EnsureSuccessStatusCode();

        var push = Assert.Single(fixture.Line.PushesTo(lineUserId));
        Assert.Contains("1460.00 บาท", push.Text);
        Assert.Equal($"action=open_invoice&invoice_id={invoiceId}", Assert.Single(push.Buttons));

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(("receipt.issued", DeliveryStatus.Sent), (row.TemplateKey, row.Status));
    }

    [Fact]
    public async Task When_LINE_refuses_the_message_falls_back_to_SMS_and_the_log_says_so()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        fixture.Line.Status = HttpStatusCode.InternalServerError;

        var response = await SendAsync(Reminder(customer.CustomerId));

        response.EnsureSuccessStatusCode();
        var result = Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("results").EnumerateArray());
        Assert.Equal("sent", result.GetProperty("status").GetString());
        Assert.Equal("sms", result.GetProperty("channel").GetString());
        Assert.Equal("line", result.GetProperty("fallback_from").GetString());

        Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));

        var rows = await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token);
        Assert.Equal(2, rows.Count);
        Assert.Equal((DeliveryStatus.Failed, DeliveryChannel.Line, DeliveryErrors.ProviderError), (rows[0].Status, rows[0].Channel, rows[0].ErrorCode));
        Assert.Equal((DeliveryStatus.Sent, DeliveryChannel.Sms, (DeliveryChannel?)DeliveryChannel.Line), (rows[1].Status, rows[1].Channel, rows[1].FallbackFrom));
        Assert.All(rows, row => Assert.Equal(DeliverySource.Manual, row.Source));
    }

    [Fact]
    public async Task A_customer_with_no_LINE_gets_one_short_SMS()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);

        (await SendAsync(Reminder(customer.CustomerId))).EnsureSuccessStatusCode();

        var text = Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));
        Assert.Contains("ลูน่า", text);
        Assert.True(text.Length <= 70, $"{text.Length} characters: {text}");

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));
        Assert.Equal((DeliveryStatus.Sent, DeliveryChannel.Sms), (row.Status, row.Channel));
        Assert.Equal($"***{customer.PhoneDigits[^4..]}", row.Recipient);
        Assert.Null(row.FallbackFrom);
    }

    [Fact]
    public async Task Asking_for_email_sends_email_only()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, email: true, cancellationToken: Token);

        (await SendAsync(Reminder(customer.CustomerId, "email"))).EnsureSuccessStatusCode();

        var mail = Assert.Single(fixture.Transports.EmailTo(customer.Email!));
        Assert.Equal("เตือนนัดหมาย", mail.Subject);
        Assert.Contains("ลูน่า", mail.TextBody);
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Empty(fixture.Transports.SmsTo(customer.PhoneDigits));
    }

    [Fact]
    public async Task Asking_for_a_channel_the_customer_cannot_receive_is_skipped_not_rerouted()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);

        var response = await SendAsync(Reminder(customer.CustomerId, "line"));

        var result = Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("results").EnumerateArray());
        Assert.Equal("skipped", result.GetProperty("status").GetString());
        Assert.Equal("no_address", result.GetProperty("error_code").GetString());
        Assert.Empty(fixture.Transports.SmsTo(customer.PhoneDigits));
    }

    [Fact]
    public async Task A_staff_send_respects_a_withdrawn_consent()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, customer.CustomerId, ConsentPurpose.ServiceMessages, false, Token);

        var body = await (await SendAsync(Reminder(customer.CustomerId))).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(0, body.GetProperty("sent").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());
        Assert.Equal("no_consent", body.GetProperty("results")[0].GetProperty("error_code").GetString());
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Empty(fixture.Transports.SmsTo(customer.PhoneDigits));
    }

    [Fact]
    public async Task A_type_switched_off_for_LINE_goes_by_SMS_instead()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var settings = fixture.Notifier(Scopes.Integration.Manage);
        (await settings.PutAsJsonAsync(
            "/staff/integrations/line",
            new { notification_types = new Dictionary<string, bool> { ["vaccine.due"] = false } },
            Token)).EnsureSuccessStatusCode();

        (await SendAsync(new
        {
            template = "vaccine.due",
            customer_ids = new[] { customer.CustomerId },
            variables = new { pet_name = "ลูน่า", date = "1 พ.ย." },
        })).EnsureSuccessStatusCode();

        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));

        (await settings.PutAsJsonAsync(
            "/staff/integrations/line",
            new { notification_types = new Dictionary<string, bool> { ["vaccine.due"] = true } },
            Token)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task With_nothing_set_up_the_message_is_logged_as_not_configured_and_the_request_still_succeeds()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConfigureLineAsync(fixture, new { channel_access_token = "" }, Token);
        fixture.Transports.SmsConfigured = false;
        fixture.Transports.EmailConfigured = false;

        var response = await SendAsync(Reminder(customer.CustomerId));

        response.EnsureSuccessStatusCode();
        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));
        Assert.Equal((DeliveryStatus.Skipped, (DeliveryChannel?)null, DeliveryErrors.NotConfigured), (row.Status, row.Channel, row.ErrorCode));

        // And the log page can find it, so "why did nothing arrive" has an answer on screen.
        using var reader = fixture.Notifier(Scopes.Notify.Read);
        var page = await reader.GetFromJsonAsync<JsonElement>($"/staff/notifications/log?customer_id={customer.CustomerId}&status=skipped", Token);
        Assert.Single(page.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public async Task A_group_send_answers_per_customer_and_its_rows_are_found_by_batch()
    {
        var onLine = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        var bySms = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);

        var body = await (await SendAsync(new
        {
            template = "clinic.message",
            customer_ids = new[] { onLine.CustomerId, bySms.CustomerId, onLine.CustomerId },
            variables = new { message = "ผลแล็บของน้องออกแล้ว" },
        })).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(2, body.GetProperty("sent").GetInt32());
        var batchId = body.GetProperty("batch_id").GetString();
        Assert.StartsWith("nbt_", batchId);

        Assert.Equal("ผลแล็บของน้องออกแล้ว", Assert.Single(fixture.Line.PushesTo(onLine.LineUserId!)).Text);

        using var reader = fixture.Notifier(Scopes.Notify.Read);
        var page = await reader.GetFromJsonAsync<JsonElement>($"/staff/notifications/log?batch_id={batchId}", Token);
        var rows = page.GetProperty("data").EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("manual", row.GetProperty("source").GetString()));
        Assert.All(rows, row => Assert.Equal("stf_test", row.GetProperty("sent_by_staff_id").GetString()));
        Assert.Equal(["line", "sms"], rows.Select(row => row.GetProperty("channel").GetString()).Order());
    }
}
