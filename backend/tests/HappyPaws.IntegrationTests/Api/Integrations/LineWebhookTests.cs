using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Notifications.Line;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>POST /webhooks/line</c>: the signature is checked before anything is read, follow and
/// unfollow start and stop pushes, a typed message gets the automatic answer, and every chat button
/// re-checks the record against the customer it came from.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class LineWebhookTests(DeliveryFixture fixture) : IAsyncLifetime
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

    private static string Event(string type, string userId, string replyToken, string? postbackData = null) =>
        JsonSerializer.Serialize(new
        {
            destination = "Ubot",
            events = new object[]
            {
                postbackData is null
                    ? new { type, mode = "active", replyToken, source = new { type = "user", userId } }
                    : new { type, mode = "active", replyToken, source = new { type = "user", userId }, postback = new { data = postbackData } },
            },
        });

    private async Task<HttpResponseMessage> PostAsync(string body, string? signature = null, bool sign = true)
    {
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/line")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        if (sign)
        {
            request.Headers.Add(
                "X-Line-Signature",
                signature ?? LineSignature.Compute(NotificationScenario.ChannelSecret, Encoding.UTF8.GetBytes(body)));
        }

        return await client.SendAsync(request, Token);
    }

    private IReadOnlyList<LineCall> RepliesTo(string replyToken) =>
        [.. fixture.Line.Replies.Where(call => call.Body.GetProperty("replyToken").GetString() == replyToken)];

    private static string NewToken() => Guid.NewGuid().ToString("N");

    [Fact]
    public async Task A_body_with_a_wrong_or_missing_signature_is_refused_and_not_read()
    {
        var replyToken = NewToken();
        var body = Event("message", "Uanyone", replyToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(body, signature: "bm90IGEgc2lnbmF0dXJl")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(body, sign: false)).StatusCode);

        // Signed with the right secret over different bytes: the signature covers the exact body.
        var other = LineSignature.Compute(NotificationScenario.ChannelSecret, Encoding.UTF8.GetBytes(body + " "));
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(body, signature: other)).StatusCode);

        Assert.Empty(RepliesTo(replyToken));
    }

    [Fact]
    public async Task With_no_secret_anywhere_every_webhook_is_refused()
    {
        await NotificationScenario.ConfigureLineAsync(fixture, new { channel_secret = "" }, Token);

        var response = await PostAsync(Event("message", "Uanyone", NewToken()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LINEs_verify_call_with_no_events_is_accepted()
    {
        var response = await PostAsync("""{"destination":"Ubot","events":[]}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_body_that_is_not_a_LINE_batch_is_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync("not json")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync("""{"hello":"world"}""")).StatusCode);
    }

    [Fact]
    public async Task A_typed_message_gets_the_automatic_answer_with_the_portal_link()
    {
        var replyToken = NewToken();

        (await PostAsync(Event("message", "Ustranger", replyToken))).EnsureSuccessStatusCode();

        var reply = Assert.Single(RepliesTo(replyToken));
        Assert.Equal($"Bearer {NotificationScenario.AccessToken}", reply.Authorization);
        Assert.Contains(DeliveryFixture.PortalUrl, reply.Text);
    }

    [Fact]
    public async Task Unfollowing_stops_LINE_pushes_and_following_again_restarts_them()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);

        (await PostAsync(Event("unfollow", customer.LineUserId!, NewToken()))).EnsureSuccessStatusCode();
        Assert.NotNull((await NotificationScenario.ReloadCustomerAsync(fixture, customer.CustomerId, Token)).LineUnfollowedAt);

        using var sender = fixture.Notifier(Scopes.Notify.Send);
        var message = new { template = "clinic.message", customer_ids = new[] { customer.CustomerId }, variables = new { message = "ทดสอบ" } };
        (await sender.PostAsJsonAsync("/staff/notifications/send", message, Token)).EnsureSuccessStatusCode();

        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));

        var followToken = NewToken();
        (await PostAsync(Event("follow", customer.LineUserId!, followToken))).EnsureSuccessStatusCode();
        Assert.Null((await NotificationScenario.ReloadCustomerAsync(fixture, customer.CustomerId, Token)).LineUnfollowedAt);
        Assert.Single(RepliesTo(followToken));

        (await sender.PostAsJsonAsync("/staff/notifications/send", message, Token)).EnsureSuccessStatusCode();
        Assert.Single(fixture.Line.PushesTo(customer.LineUserId!));
    }

    [Fact]
    public async Task A_follow_from_someone_unknown_is_welcomed_and_nothing_is_created()
    {
        var replyToken = NewToken();

        (await PostAsync(Event("follow", $"U{Guid.NewGuid():N}", replyToken))).EnsureSuccessStatusCode();

        Assert.Single(RepliesTo(replyToken));
    }

    [Fact]
    public async Task Confirm_from_a_chat_button_confirms_the_customers_own_appointment()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 4, Token);
        var replyToken = NewToken();

        var data = LinePostbackData.Build(LinePostbackActions.ConfirmAppointment, (LinePostbackData.AppointmentId, appointmentId));
        (await PostAsync(Event("postback", lineUserId, replyToken, data))).EnsureSuccessStatusCode();

        var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
        Assert.Equal(AppointmentStatus.Confirmed, appointment!.Status);
        Assert.Contains("ยืนยันนัด", Assert.Single(RepliesTo(replyToken)).Text);
    }

    [Fact]
    public async Task A_button_naming_someone_elses_appointment_changes_nothing()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, mine.CustomerId, Token);
        var theirAppointment = await BookingScenario.BookAsStaffAsync(fixture, theirs, 4, Token);
        var replyToken = NewToken();

        foreach (var action in new[] { LinePostbackActions.ConfirmAppointment, LinePostbackActions.CancelAppointment })
        {
            var data = LinePostbackData.Build(action, (LinePostbackData.AppointmentId, theirAppointment), (LinePostbackData.Confirmed, "1"));
            (await PostAsync(Event("postback", lineUserId, replyToken, data))).EnsureSuccessStatusCode();
        }

        var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, theirAppointment, Token);
        Assert.Equal(AppointmentStatus.Booked, appointment!.Status);
        Assert.All(RepliesTo(replyToken), reply => Assert.Contains("ไม่พบ", reply.Text));
    }

    [Fact]
    public async Task Cancel_asks_first_and_cancels_on_the_second_tap()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 5, Token);

        var askToken = NewToken();
        var first = LinePostbackData.Build(LinePostbackActions.CancelAppointment, (LinePostbackData.AppointmentId, appointmentId));
        (await PostAsync(Event("postback", lineUserId, askToken, first))).EnsureSuccessStatusCode();

        Assert.Equal(AppointmentStatus.Booked, (await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token))!.Status);
        var confirmData = Assert.Single(Assert.Single(RepliesTo(askToken)).Buttons);

        var cancelToken = NewToken();
        (await PostAsync(Event("postback", lineUserId, cancelToken, confirmData))).EnsureSuccessStatusCode();

        var cancelled = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
        Assert.Equal(AppointmentStatus.Cancelled, cancelled!.Status);
        Assert.Equal("ยกเลิกผ่านปุ่มใน LINE", cancelled.CancellationReason);
        Assert.Contains("ยกเลิกนัดเรียบร้อย", Assert.Single(RepliesTo(cancelToken)).Text);
    }

    [Fact]
    public async Task A_button_from_a_LINE_user_no_customer_carries_does_nothing_but_explain()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 6, Token);
        var replyToken = NewToken();

        var data = LinePostbackData.Build(LinePostbackActions.ConfirmAppointment, (LinePostbackData.AppointmentId, appointmentId));
        (await PostAsync(Event("postback", $"U{Guid.NewGuid():N}", replyToken, data))).EnsureSuccessStatusCode();

        Assert.Equal(AppointmentStatus.Booked, (await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token))!.Status);
        Assert.Contains(DeliveryFixture.PortalUrl, Assert.Single(RepliesTo(replyToken)).Text);
    }

    [Fact]
    public async Task View_queue_answers_with_the_customers_number_today()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 3, Token);
        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.CheckIn, Scopes.Queue.Call], [clinic.BranchId]));
        var ticketId = await CheckInAsync(staff, appointmentId, Token);
        (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/call", new { }, Token)).EnsureSuccessStatusCode();

        // The queue-call push carries the button; pressing it comes back here.
        var button = Assert.Single(Assert.Single(fixture.Line.PushesTo(lineUserId)).Buttons);
        var replyToken = NewToken();
        (await PostAsync(Event("postback", lineUserId, replyToken, button))).EnsureSuccessStatusCode();

        var reply = Assert.Single(RepliesTo(replyToken)).Text;
        Assert.StartsWith("คิว ", reply);
        Assert.Contains("กำลังเรียก", reply);

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(DeliveryChannel.Line, row.Channel);
    }
}
