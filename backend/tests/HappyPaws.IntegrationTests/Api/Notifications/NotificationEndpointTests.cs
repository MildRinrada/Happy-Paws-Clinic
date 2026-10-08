using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Notifications;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Notifications;

/// <summary>
/// Who may use the notification and LINE endpoints, what they refuse, the template list, and the
/// delivery log's branch filter. On the shared clinic: nothing here changes the LINE settings row.
/// </summary>
[Collection(nameof(ApiCollection))]
public class NotificationEndpointTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object Reminder = new
    {
        template = "appointment.reminder",
        customer_ids = new[] { "cus_x" },
        variables = new { pet_name = "ลูน่า", date = "12 ต.ค.", time = "10:30" },
    };

    public static TheoryData<string, string, string> Endpoints => new()
    {
        { "POST", "/staff/notifications/send", Scopes.Notify.Send },
        { "GET", "/staff/notifications/templates", Scopes.Notify.Manage },
        { "GET", "/staff/notifications/log", Scopes.Notify.Read },
        { "PUT", "/staff/integrations/line", Scopes.Integration.Manage },
        { "POST", "/staff/integrations/line/broadcast", Scopes.Notify.Broadcast },
    };

    private static HttpRequestMessage Request(string method, string path) =>
        new(new HttpMethod(method), path) { Content = method == "GET" ? null : JsonContent.Create(new { }) };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Each_endpoint_needs_its_own_scope(string method, string path, string scope)
    {
        var others = NotificationScenario.AllNotifyScopes.Where(other => other != scope).ToArray();
        using var client = fixture.Notifier(others);

        var response = await client.SendAsync(Request(method, path), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Each_endpoint_refuses_an_anonymous_caller(string method, string path, string scope)
    {
        _ = scope;
        using var client = fixture.CreateClient();

        var response = await client.SendAsync(Request(method, path), Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, object> BadSends => new()
    {
        { "template", new { template = "receipt.issued", customer_ids = new[] { "cus_x" }, variables = new { receipt_number = "1", amount = "1" } } },
        { "template", new { template = "no.such.template", customer_ids = new[] { "cus_x" } } },
        { "customer_ids", new { template = "clinic.message", customer_ids = Array.Empty<string>(), variables = new { message = "x" } } },
        { "channel", new { template = "clinic.message", customer_ids = new[] { "cus_x" }, channel = "fax", variables = new { message = "x" } } },
        { "variables.pet_name", new { template = "appointment.reminder", customer_ids = new[] { "cus_x" }, variables = new { date = "1", time = "2" } } },
        { "variables.colour", new { template = "clinic.message", customer_ids = new[] { "cus_x" }, variables = new { message = "x", colour = "red" } } },
        { "variables.message", new { template = "clinic.message", customer_ids = new[] { "cus_x" }, variables = new { message = new string('ก', 501) } } },
    };

    [Theory]
    [MemberData(nameof(BadSends))]
    public async Task A_send_that_does_not_fit_its_template_is_refused_before_anything_goes_out(string field, object body)
    {
        using var client = fixture.Notifier(Scopes.Notify.Send);

        var response = await client.PostAsJsonAsync("/staff/notifications/send", body, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Fact]
    public async Task A_send_to_a_customer_who_does_not_exist_is_404()
    {
        using var client = fixture.Notifier(Scopes.Notify.Send);

        var response = await client.PostAsJsonAsync("/staff/notifications/send", Reminder, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_template_list_shows_the_values_the_consent_and_the_wording()
    {
        using var client = fixture.Notifier(Scopes.Notify.Manage);

        var templates = (await client.GetFromJsonAsync<JsonElement>("/staff/notifications/templates", Token))
            .GetProperty("data").EnumerateArray().ToList();

        var reminder = templates.Single(item => item.GetProperty("key").GetString() == "appointment.reminder");
        Assert.True(reminder.GetProperty("staff_sendable").GetBoolean());
        Assert.Equal("service_messages", reminder.GetProperty("purpose").GetString());
        Assert.Equal(
            ["pet_name", "date", "time", "vet_name"],
            reminder.GetProperty("variables").EnumerateArray().Select(variable => variable.GetProperty("name").GetString()));
        Assert.Contains("{vet_name}", reminder.GetProperty("bodies").GetProperty("line").GetString());

        var announcement = templates.Single(item => item.GetProperty("key").GetString() == "clinic.announcement");
        Assert.Equal("marketing", announcement.GetProperty("purpose").GetString());
        Assert.False(announcement.GetProperty("staff_sendable").GetBoolean());

        // The ones staff can send come first.
        Assert.True(templates[0].GetProperty("staff_sendable").GetBoolean());
    }

    [Theory]
    [InlineData("{\"channel_id\":\"abc\"}", "channel_id")]
    [InlineData("{\"notification_types\":{\"no.such.type\":false}}", "notification_types")]
    [InlineData("{\"broadcast_monthly_limit\":-5}", "broadcast_monthly_limit")]
    [InlineData("{\"channel_secret\":\"has space\"}", "channel_secret")]
    public async Task LINE_settings_that_cannot_be_right_are_refused(string json, string field)
    {
        using var client = fixture.Notifier(Scopes.Integration.Manage);

        var response = await client.PutAsync(
            "/staff/integrations/line", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Theory]
    [InlineData("{\"customer_ids\":[\"cus_x\"],\"message\":\"\"}", "message")]
    [InlineData("{\"customer_ids\":[],\"message\":\"ประกาศ\"}", "customer_ids")]
    public async Task A_broadcast_without_a_message_or_an_audience_is_refused(string json, string field)
    {
        using var client = fixture.Notifier(Scopes.Notify.Broadcast);

        var response = await client.PostAsync(
            "/staff/integrations/line/broadcast", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Fact]
    public async Task A_log_filter_that_is_not_a_known_value_is_refused()
    {
        using var client = fixture.Notifier(Scopes.Notify.Read);

        var response = await client.GetAsync("/staff/notifications/log?status=delivered", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_queue_call_to_an_owner_who_opted_out_is_on_the_log_as_skipped_for_consent()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, Domain.Customers.ConsentPurpose.ServiceMessages, false, Token);

        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 3, Token);
        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.CheckIn, Scopes.Queue.Call], [clinic.BranchId]));
        var ticketId = await CheckInAsync(staff, appointmentId, Token);
        (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/call", new { }, Token)).EnsureSuccessStatusCode();

        // The channel never saw it, and the log says why.
        Assert.Empty(fixture.Notifications.SentTo(clinic.CustomerId));
        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(DeliveryStatus.Skipped, row.Status);
        Assert.Equal(DeliveryErrors.NoConsent, row.ErrorCode);
        Assert.Equal("queue.called", row.TemplateKey);
        Assert.Equal(clinic.BranchId, row.BranchId);

        using var reader = fixture.Notifier(Scopes.Notify.Read);
        var page = await reader.GetFromJsonAsync<JsonElement>($"/staff/notifications/log?customer_id={clinic.CustomerId}", Token);
        var shown = Assert.Single(page.GetProperty("data").EnumerateArray());
        Assert.Equal("skipped", shown.GetProperty("status").GetString());
        Assert.Equal("no_consent", shown.GetProperty("error_code").GetString());
        Assert.Equal("คุณลูกค้า ทดสอบ", shown.GetProperty("customer_name").GetString());
    }

    [Fact]
    public async Task A_branch_bound_reader_sees_their_branch_and_clinic_wide_rows_only()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        var mine = $"brn_mine_{Guid.NewGuid():N}"[..30];
        var theirs = $"brn_theirs_{Guid.NewGuid():N}"[..30];

        using (var scope = fixture.CreateScope())
        {
            var log = scope.ServiceProvider.GetRequiredService<IDeliveryLog>();

            foreach (var branch in new[] { mine, theirs, null })
            {
                await log.RecordAsync(
                    NotificationDelivery.Record(
                        customer.CustomerId, "clinic.message", DeliveryStatus.Sent, DeliverySource.Manual, DeliveryChannel.Sms, "***0000", branchId: branch),
                    Token);
            }
        }

        using var reader = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Notify.Read], branches: [mine]));
        var page = await reader.GetFromJsonAsync<JsonElement>($"/staff/notifications/log?customer_id={customer.CustomerId}", Token);

        var branches = page.GetProperty("data").EnumerateArray()
            .Select(row => row.TryGetProperty("branch_id", out var branch) ? branch.GetString() : null)
            .ToList();

        Assert.Equal(2, branches.Count);
        Assert.Contains(mine, branches);
        Assert.Contains(null, branches);
        Assert.DoesNotContain(theirs, branches);
    }
}
