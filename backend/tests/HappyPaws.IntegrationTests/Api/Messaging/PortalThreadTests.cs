using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Api.Extensions;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Messaging;

/// <summary>
/// <c>GET /portal/thread</c> and <c>POST /portal/thread/messages</c> (contract extension § 24): the
/// signed-in customer reads and writes their own thread, the one staff answer at
/// <c>/staff/threads</c>, and a reply to a thread last written from the portal reaches the web
/// inbox and the usual LINE, SMS or email push.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class PortalThreadTests(DeliveryFixture fixture) : IAsyncLifetime
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

    /// <summary>A portal session of the customer, on a user id of its own so rate limit windows never meet.</summary>
    private HttpClient Portal(string customerId) =>
        fixture.Authenticated(TestTokens.Customer(fixture, userId: $"usr_{customerId}", customerId: customerId));

    private HttpClient Staff(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: scopes.Length == 0 ? [Scopes.Message.Read, Scopes.Message.Write] : scopes));

    private static Task<HttpResponseMessage> WriteAsync(HttpClient client, string? text) =>
        client.PostAsJsonAsync("/portal/thread/messages", new { text }, Token);

    private static async Task<JsonElement> ReadAsync(HttpClient client, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"/portal/thread{query}", Token);

    private static IReadOnlyList<string?> Texts(JsonElement thread) =>
        [.. thread.GetProperty("data").EnumerateArray().Select(item => item.GetProperty("text").GetString())];

    private async Task<JsonElement> StaffThreadAsync(HttpClient client, string customerId)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/staff/threads?customer_id={customerId}&messages=20", Token);
        return Assert.Single(page.GetProperty("data").EnumerateArray());
    }

    private async Task<bool> HasThreadAsync(string customerId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        return await db.Set<MessageThread>().AnyAsync(thread => thread.CustomerId == customerId, Token);
    }

    private static string? NextCursor(JsonElement thread) =>
        thread.GetProperty("page").TryGetProperty("next_cursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;

    [Fact]
    public async Task Reading_before_the_first_message_answers_an_empty_thread_and_creates_nothing()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);

        var thread = await ReadAsync(client);

        Assert.False(thread.TryGetProperty("id", out _));
        Assert.False(thread.TryGetProperty("status", out _));
        Assert.Empty(thread.GetProperty("data").EnumerateArray());
        Assert.Null(NextCursor(thread));
        Assert.False(await HasThreadAsync(customer.CustomerId));
    }

    [Fact]
    public async Task The_first_message_opens_the_thread_on_the_portal_channel_and_staff_see_it()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);

        var response = await WriteAsync(client, "  น้องแมวไม่กินข้าวค่ะ  ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.StartsWith("msg_", message.GetProperty("id").GetString());
        Assert.Equal(("inbound", "portal", "text", "น้องแมวไม่กินข้าวค่ะ"), (
            message.GetProperty("direction").GetString(),
            message.GetProperty("channel").GetString(),
            message.GetProperty("kind").GetString(),
            message.GetProperty("text").GetString()));

        var thread = await ReadAsync(client);
        Assert.StartsWith("thr_", thread.GetProperty("id").GetString());
        Assert.Equal("awaiting_reply", thread.GetProperty("status").GetString());
        Assert.Equal(["น้องแมวไม่กินข้าวค่ะ"], Texts(thread));

        // No automatic answer on any channel: the portal page says it itself.
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Empty(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));

        using var staff = Staff();
        var staffView = await StaffThreadAsync(staff, customer.CustomerId);
        Assert.Equal(thread.GetProperty("id").GetString(), staffView.GetProperty("id").GetString());
        Assert.Equal("awaiting_reply", staffView.GetProperty("status").GetString());
        Assert.Equal("portal", staffView.GetProperty("reply_channel").GetString());
        Assert.Equal("portal", Assert.Single(staffView.GetProperty("messages").EnumerateArray()).GetProperty("channel").GetString());
    }

    [Fact]
    public async Task Portal_and_LINE_messages_share_one_thread_and_the_pages_walk_back_without_gaps()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);

        foreach (var text in new[] { "หนึ่ง", "สอง", "สาม" })
        {
            (await WriteAsync(client, text)).EnsureSuccessStatusCode();
        }

        var first = await ReadAsync(client, "?limit=2");
        Assert.Equal(["สาม", "สอง"], Texts(first));
        var cursor = NextCursor(first);
        Assert.NotNull(cursor);

        var second = await ReadAsync(client, $"?limit=2&cursor={cursor}");
        Assert.Equal(["หนึ่ง"], Texts(second));
        Assert.Null(NextCursor(second));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/portal/thread?cursor=bm9wZQ", Token)).StatusCode);

        using var staff = Staff();
        Assert.Equal(3, (await StaffThreadAsync(staff, customer.CustomerId)).GetProperty("message_count").GetInt32());
    }

    [Fact]
    public async Task Bad_input_is_refused_and_nothing_is_stored()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);

        foreach (var text in new[] { "   ", null, new string('ก', 1_001) })
        {
            var refused = await WriteAsync(client, text);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
            var body = await refused.Content.ReadFromJsonAsync<JsonElement>(Token);
            Assert.Equal("validation_failed", body.GetProperty("error").GetProperty("code").GetString());
        }

        Assert.False(await HasThreadAsync(customer.CustomerId));

        // The limit itself is fine, counted after trimming.
        Assert.Equal(HttpStatusCode.Created, (await WriteAsync(client, $"  {new string('ก', 1_000)}  ")).StatusCode);
    }

    [Fact]
    public async Task Only_a_customer_session_may_read_or_write_and_staff_are_refused()
    {
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/portal/thread", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await WriteAsync(anonymous, "สวัสดี")).StatusCode);

        using var staff = Staff(Scopes.Message.Read, Scopes.Message.Write);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/portal/thread", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await WriteAsync(staff, "สวัสดี")).StatusCode);

        using var owner = Staff(Scopes.Wildcard);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/portal/thread", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await WriteAsync(owner, "สวัสดี")).StatusCode);
    }

    [Fact]
    public async Task Each_customer_sees_and_writes_only_their_own_thread()
    {
        var alice = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        var bob = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var aliceClient = Portal(alice.CustomerId);
        using var bobClient = Portal(bob.CustomerId);

        (await WriteAsync(aliceClient, "ของอลิซ")).EnsureSuccessStatusCode();

        var bobs = await ReadAsync(bobClient);
        Assert.False(bobs.TryGetProperty("id", out _));
        Assert.Empty(bobs.GetProperty("data").EnumerateArray());

        (await WriteAsync(bobClient, "ของบ็อบ")).EnsureSuccessStatusCode();

        var alices = await ReadAsync(aliceClient);
        bobs = await ReadAsync(bobClient);
        Assert.Equal(["ของอลิซ"], Texts(alices));
        Assert.Equal(["ของบ็อบ"], Texts(bobs));
        Assert.NotEqual(alices.GetProperty("id").GetString(), bobs.GetProperty("id").GetString());
    }

    [Fact]
    public async Task A_staff_reply_to_a_portal_thread_reaches_the_chat_the_web_inbox_and_the_usual_push()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);
        (await WriteAsync(client, "ขอสอบถามเรื่องวัคซีนค่ะ")).EnsureSuccessStatusCode();
        var threadId = (await ReadAsync(client)).GetProperty("id").GetString()!;

        using var staff = Staff();
        var response = await staff.PostAsJsonAsync($"/staff/threads/{threadId}/messages", new { text = "พามาได้ทุกวันค่ะ" }, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var replyId = reply.GetProperty("id").GetString()!;

        // "portal" names no channel to try first, so the dispatcher takes its usual order: LINE.
        Assert.Equal(("sent", "line"), (reply.GetProperty("delivery").GetProperty("status").GetString(), reply.GetProperty("delivery").GetProperty("channel").GetString()));
        Assert.Equal("พามาได้ทุกวันค่ะ", Assert.Single(fixture.Line.PushesTo(customer.LineUserId!)).Text);

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));
        Assert.Equal(("thread.reply", replyId, DeliveryChannel.Line), (row.TemplateKey, row.BatchId, row.Channel));

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var inbox = await db.Set<InboxNotification>().SingleAsync(item => item.CustomerId == customer.CustomerId, Token);
            Assert.Equal(("thread.reply", "พามาได้ทุกวันค่ะ"), (inbox.TemplateKey, inbox.Body));
        }

        // The chat shows the answer, without the staff id or the delivery details.
        var thread = await ReadAsync(client);
        Assert.Equal("replied", thread.GetProperty("status").GetString());
        var answer = thread.GetProperty("data").EnumerateArray().First();
        Assert.Equal((replyId, "outbound", "พามาได้ทุกวันค่ะ"), (answer.GetProperty("id").GetString(), answer.GetProperty("direction").GetString(), answer.GetProperty("text").GetString()));
        Assert.False(answer.TryGetProperty("sent_by_staff_id", out _));
        Assert.False(answer.TryGetProperty("delivery", out _));
        Assert.False(answer.TryGetProperty("channel", out _));
        Assert.DoesNotContain("stf_test", await (await client.GetAsync("/portal/thread", Token)).Content.ReadAsStringAsync(Token), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_customer_without_LINE_gets_the_reply_to_a_portal_thread_by_SMS()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);
        (await WriteAsync(client, "สอบถามค่ะ")).EnsureSuccessStatusCode();
        var threadId = (await ReadAsync(client)).GetProperty("id").GetString()!;

        using var staff = Staff();
        var reply = await (await staff.PostAsJsonAsync($"/staff/threads/{threadId}/messages", new { text = "ตอบทาง SMS" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("sms", reply.GetProperty("delivery").GetProperty("channel").GetString());
        Assert.Contains(fixture.Transports.SmsTo(customer.PhoneDigits), text => text.Contains("ตอบทาง SMS", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Without_consent_the_reply_is_not_pushed_or_put_in_the_inbox_but_the_chat_still_shows_it()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var client = Portal(customer.CustomerId);
        (await WriteAsync(client, "สอบถามค่ะ")).EnsureSuccessStatusCode();
        var threadId = (await ReadAsync(client)).GetProperty("id").GetString()!;
        await NotificationScenario.ConsentAsync(fixture, customer.CustomerId, ConsentPurpose.ServiceMessages, granted: false, Token);

        using var staff = Staff();
        var reply = await (await staff.PostAsJsonAsync($"/staff/threads/{threadId}/messages", new { text = "ตอบในหน้าแชท" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(("skipped", "no_consent"), (reply.GetProperty("delivery").GetProperty("status").GetString(), reply.GetProperty("delivery").GetProperty("error_code").GetString()));
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            Assert.False(await db.Set<InboxNotification>().AnyAsync(item => item.CustomerId == customer.CustomerId, Token));
        }

        Assert.Equal(["ตอบในหน้าแชท", "สอบถามค่ะ"], Texts(await ReadAsync(client)));
    }

    [Fact]
    public async Task A_portal_thread_is_filed_under_the_branch_the_customer_last_booked_at()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var startsAt = clinic.ShiftStartsAt.AddHours(3);
            db.Set<Appointment>().Add(Appointment.Book(clinic.BranchId, clinic.CustomerId, clinic.PetId, clinic.ServiceId, clinic.StaffId, startsAt, startsAt.AddMinutes(30)));
            await db.SaveChangesAsync(Token);
        }

        using var client = Portal(clinic.CustomerId);
        (await WriteAsync(client, "จะเข้าไปตามนัดค่ะ")).EnsureSuccessStatusCode();

        using var staff = Staff();
        Assert.Equal(clinic.BranchId, (await StaffThreadAsync(staff, clinic.CustomerId)).GetProperty("branch_id").GetString());
    }

    [Fact]
    public async Task Merging_two_records_folds_their_portal_messages_into_the_survivors_chat()
    {
        var surviving = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        var duplicate = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var survivingClient = Portal(surviving.CustomerId);
        using var duplicateClient = Portal(duplicate.CustomerId);
        (await WriteAsync(survivingClient, "จากเรกคอร์ดที่เก็บ")).EnsureSuccessStatusCode();
        (await WriteAsync(duplicateClient, "จากเรกคอร์ดซ้ำ")).EnsureSuccessStatusCode();

        using var counter = Staff(Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Customer.Merge, Scopes.Message.Read);
        var merged = await counter.PostAsJsonAsync(
            $"/staff/customers/{surviving.CustomerId}/merge",
            new { duplicate_customer_id = duplicate.CustomerId },
            Token);
        merged.EnsureSuccessStatusCode();
        Assert.Equal(1, (await merged.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("moved").GetProperty("thread_messages").GetInt32());

        Assert.Equal(["จากเรกคอร์ดซ้ำ", "จากเรกคอร์ดที่เก็บ"], Texts(await ReadAsync(survivingClient)));
        Assert.Empty((await ReadAsync(duplicateClient)).GetProperty("data").EnumerateArray());

        // A session minted for the merged record cannot open a second thread for it.
        var refused = await WriteAsync(duplicateClient, "ยังเขียนได้ไหม");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.False(await HasThreadAsync(duplicate.CustomerId));
    }

    [Fact]
    public void Writing_is_under_the_messaging_rate_limit_and_reading_is_not()
    {
        using var scope = fixture.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        RouteEndpoint Find(string pattern, string method) => Assert.Single(endpoints, endpoint =>
            endpoint.RoutePattern.RawText?.TrimStart('/') == pattern
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);

        Assert.Equal(RateLimitingExtensions.MessagingPolicy, Find("portal/thread/messages", "POST").Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Null(Find("portal/thread", "GET").Metadata.GetMetadata<EnableRateLimitingAttribute>());
    }
}
