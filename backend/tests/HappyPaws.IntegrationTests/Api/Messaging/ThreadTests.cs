using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Api.Extensions;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Notifications.Line;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Messaging;

/// <summary>
/// <c>GET /staff/threads</c> and <c>POST /staff/threads/{id}/messages</c>: a LINE message from an
/// address on a customer's record starts or continues their thread, and a staff reply goes back
/// through the dispatcher on the channel the customer used last, with consent and the delivery log
/// applying as to every other message.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class ThreadTests(DeliveryFixture fixture) : IAsyncLifetime
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

    private static string NewId() => Guid.NewGuid().ToString("N");

    private HttpClient Staff(IReadOnlyCollection<string>? branches = null, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            scopes: scopes.Length == 0 ? [Scopes.Message.Read, Scopes.Message.Write] : scopes,
            branches: branches));

    /// <summary>A LINE <c>message</c> event; answers the reply token used.</summary>
    private async Task<string> LineMessageAsync(string userId, object message, string? replyToken = null)
    {
        replyToken ??= NewId();
        var body = JsonSerializer.Serialize(new
        {
            destination = "Ubot",
            events = new[] { new { type = "message", mode = "active", replyToken, source = new { type = "user", userId }, message } },
        });

        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/line")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Line-Signature", LineSignature.Compute(NotificationScenario.ChannelSecret, Encoding.UTF8.GetBytes(body)));

        (await client.SendAsync(request, Token)).EnsureSuccessStatusCode();
        return replyToken;
    }

    private Task<string> TextAsync(string userId, string text, string? messageId = null) =>
        LineMessageAsync(userId, new { id = messageId ?? NewId(), type = "text", text });

    private IReadOnlyList<LineCall> RepliesTo(string replyToken) =>
        [.. fixture.Line.Replies.Where(call => call.Body.GetProperty("replyToken").GetString() == replyToken)];

    private async Task<JsonElement?> ThreadOfAsync(HttpClient client, string customerId, int messages = 20)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/staff/threads?customer_id={customerId}&messages={messages}", Token);
        var data = page.GetProperty("data").EnumerateArray().ToList();
        return data.Count == 0 ? null : Assert.Single(data);
    }

    private static Task<HttpResponseMessage> ReplyAsync(HttpClient client, string threadId, string? text) =>
        client.PostAsJsonAsync($"/staff/threads/{threadId}/messages", new { text }, Token);

    private async Task<Reachable> CustomerWithThreadAsync(string text = "สวัสดีค่ะ น้องไม่กินข้าว")
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await TextAsync(customer.LineUserId!, text);
        return customer;
    }

    private async Task<string> ThreadIdAsync(string customerId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        return await db.Set<MessageThread>().Where(thread => thread.CustomerId == customerId).Select(thread => thread.Id).SingleAsync(Token);
    }

    private async Task FileUnderBranchAsync(string threadId, string branchId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        await db.Set<MessageThread>().Where(thread => thread.Id == threadId)
            .ExecuteUpdateAsync(set => set.SetProperty(thread => thread.BranchId, branchId), Token);
    }

    private static string ErrorCode(JsonElement body) => body.GetProperty("error").GetProperty("code").GetString()!;

    [Fact]
    public async Task A_LINE_message_from_a_customers_address_opens_their_thread_and_is_acknowledged_once()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);

        var first = await TextAsync(customer.LineUserId!, "สวัสดีค่ะ");
        var second = await TextAsync(customer.LineUserId!, "น้องไม่กินข้าวสองวันแล้ว");

        var ack = Assert.Single(RepliesTo(first));
        Assert.Contains("ได้รับข้อความแล้ว", ack.Text);
        Assert.Empty(RepliesTo(second));

        using var client = Staff();
        var thread = (await ThreadOfAsync(client, customer.CustomerId))!.Value;
        Assert.StartsWith("thr_", thread.GetProperty("id").GetString());
        Assert.Equal("awaiting_reply", thread.GetProperty("status").GetString());
        Assert.Equal(2, thread.GetProperty("unread_count").GetInt32());
        Assert.Equal(2, thread.GetProperty("message_count").GetInt32());
        Assert.Equal("line", thread.GetProperty("reply_channel").GetString());
        Assert.Equal("คุณลูกค้า แจ้งเตือน", thread.GetProperty("customer_name").GetString());
        Assert.Equal("น้องไม่กินข้าวสองวันแล้ว", thread.GetProperty("last_message").GetProperty("text").GetString());

        var messages = thread.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(["สวัสดีค่ะ", "น้องไม่กินข้าวสองวันแล้ว"], messages.Select(item => item.GetProperty("text").GetString()));
        Assert.All(messages, item =>
        {
            Assert.Equal("inbound", item.GetProperty("direction").GetString());
            Assert.Equal("line", item.GetProperty("channel").GetString());
            Assert.Equal("text", item.GetProperty("kind").GetString());
        });
        Assert.False(thread.GetProperty("has_more_messages").GetBoolean());
    }

    [Fact]
    public async Task A_webhook_delivered_twice_is_stored_once_and_pictures_and_places_are_recorded_by_kind()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        var messageId = NewId();

        await TextAsync(customer.LineUserId!, "ซ้ำ", messageId);
        await TextAsync(customer.LineUserId!, "ซ้ำ", messageId);
        await LineMessageAsync(customer.LineUserId!, new { id = NewId(), type = "sticker", packageId = "1", stickerId = "2" });
        await LineMessageAsync(customer.LineUserId!, new { id = NewId(), type = "location", title = "บ้าน", address = "สุขุมวิท 39", latitude = 13.7, longitude = 100.5 });

        using var client = Staff();
        var thread = (await ThreadOfAsync(client, customer.CustomerId))!.Value;
        var messages = thread.GetProperty("messages").EnumerateArray().ToList();

        Assert.Equal(3, messages.Count);
        Assert.Equal(["text", "sticker", "location"], messages.Select(item => item.GetProperty("kind").GetString()));
        Assert.False(messages[1].TryGetProperty("text", out _));
        Assert.Equal("บ้าน สุขุมวิท 39", messages[2].GetProperty("text").GetString());
    }

    [Fact]
    public async Task A_message_from_an_address_on_no_record_is_not_kept_and_gets_the_automatic_answer()
    {
        var stranger = $"U{NewId()}";
        var messageId = NewId();

        var replyToken = await TextAsync(stranger, "มีใครอยู่ไหม", messageId);

        Assert.Contains(DeliveryFixture.PortalUrl, Assert.Single(RepliesTo(replyToken)).Text);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.False(await db.Set<ThreadMessage>().AnyAsync(message => message.ExternalId == messageId, Token));
    }

    [Fact]
    public async Task A_reply_goes_back_on_LINE_through_the_dispatcher_and_the_delivery_log()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);
        using var client = Staff();

        var response = await ReplyAsync(client, threadId, "  พาน้องมาตรวจได้เลยค่ะ  ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var messageId = reply.GetProperty("id").GetString()!;
        Assert.StartsWith("msg_", messageId);
        Assert.Equal("outbound", reply.GetProperty("direction").GetString());
        Assert.Equal("พาน้องมาตรวจได้เลยค่ะ", reply.GetProperty("text").GetString());
        Assert.Equal("stf_test", reply.GetProperty("sent_by_staff_id").GetString());
        Assert.Equal("sent", reply.GetProperty("delivery").GetProperty("status").GetString());
        Assert.Equal("line", reply.GetProperty("delivery").GetProperty("channel").GetString());

        Assert.Equal("พาน้องมาตรวจได้เลยค่ะ", Assert.Single(fixture.Line.PushesTo(customer.LineUserId!)).Text);

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));
        Assert.Equal(("thread.reply", DeliverySource.Manual, messageId, "stf_test"), (row.TemplateKey, row.Source, row.BatchId, row.SentByStaffId));

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            Assert.True(await db.Set<InboxNotification>().AnyAsync(item => item.CustomerId == customer.CustomerId && item.TemplateKey == "thread.reply", Token));
        }

        var thread = (await ThreadOfAsync(client, customer.CustomerId))!.Value;
        Assert.Equal("replied", thread.GetProperty("status").GetString());
        Assert.Equal(0, thread.GetProperty("unread_count").GetInt32());
        var last = thread.GetProperty("messages").EnumerateArray().Last();
        Assert.Equal(messageId, last.GetProperty("id").GetString());
        Assert.Equal("sent", last.GetProperty("delivery").GetProperty("status").GetString());

        // The customer writes again: awaiting a reply once more, and acknowledged again.
        var again = await TextAsync(customer.LineUserId!, "ขอบคุณค่ะ");
        Assert.Single(RepliesTo(again));
        thread = (await ThreadOfAsync(client, customer.CustomerId))!.Value;
        Assert.Equal("awaiting_reply", thread.GetProperty("status").GetString());
        Assert.Equal(1, thread.GetProperty("unread_count").GetInt32());
    }

    [Fact]
    public async Task A_reply_tries_the_channel_the_customer_wrote_on_last_first()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);

        // As if their latest message had come by SMS: the reply goes there, although LINE works.
        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<MessageThread>().Where(thread => thread.Id == threadId)
                .ExecuteUpdateAsync(set => set.SetProperty(thread => thread.LastInboundChannel, MessageChannel.Sms), Token);
        }

        using var client = Staff();
        Assert.Equal("sms", (await ThreadOfAsync(client, customer.CustomerId))!.Value.GetProperty("reply_channel").GetString());

        var reply = await (await ReplyAsync(client, threadId, "ส่งทาง SMS")).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("sms", reply.GetProperty("delivery").GetProperty("channel").GetString());
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));
    }

    [Fact]
    public async Task A_customer_who_blocked_the_LINE_account_gets_the_reply_by_SMS()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var record = await db.Set<Customer>().FirstAsync(item => item.Id == customer.CustomerId, Token);
            record.MarkLineUnfollowed(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(Token);
        }

        using var client = Staff();
        var reply = await (await ReplyAsync(client, threadId, "ผลแล็บออกแล้วค่ะ")).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("sent", reply.GetProperty("delivery").GetProperty("status").GetString());
        Assert.Equal("sms", reply.GetProperty("delivery").GetProperty("channel").GetString());
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Contains(fixture.Transports.SmsTo(customer.PhoneDigits), text => text.Contains("ผลแล็บออกแล้วค่ะ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_failed_LINE_push_falls_back_to_SMS_and_says_so()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);
        fixture.Line.Status = HttpStatusCode.InternalServerError;

        using var client = Staff();
        var reply = await (await ReplyAsync(client, threadId, "โทรกลับนะคะ")).Content.ReadFromJsonAsync<JsonElement>(Token);

        var delivery = reply.GetProperty("delivery");
        Assert.Equal(("sent", "sms", "line"), (delivery.GetProperty("status").GetString(), delivery.GetProperty("channel").GetString(), delivery.GetProperty("fallback_from").GetString()));
    }

    [Fact]
    public async Task A_customer_who_withdrew_consent_is_not_sent_the_reply_and_the_answer_says_why()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);
        await NotificationScenario.ConsentAsync(fixture, customer.CustomerId, ConsentPurpose.ServiceMessages, granted: false, Token);

        using var client = Staff();
        var response = await ReplyAsync(client, threadId, "ทดสอบความยินยอม");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var delivery = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("delivery");
        Assert.Equal("skipped", delivery.GetProperty("status").GetString());
        Assert.Equal("no_consent", delivery.GetProperty("error_code").GetString());
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Empty(fixture.Transports.SmsTo(customer.PhoneDigits));

        // Stored all the same: the thread is the record of what the clinic answered.
        var thread = (await ThreadOfAsync(client, customer.CustomerId))!.Value;
        Assert.Equal("replied", thread.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Bad_input_is_refused_before_anything_is_stored_or_sent()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);
        using var client = Staff();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ReplyAsync(client, threadId, "   ")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ReplyAsync(client, threadId, null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ReplyAsync(client, threadId, new string('ก', 1_001))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReplyAsync(client, "thr_missing", "สวัสดี")).StatusCode);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/threads?status=open", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/threads?messages=101", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/threads?cursor=bm9wZQ", Token)).StatusCode);

        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
        Assert.Empty(await NotificationScenario.DeliveriesAsync(fixture, customer.CustomerId, Token));
        Assert.Equal(1, (await ThreadOfAsync(client, customer.CustomerId))!.Value.GetProperty("message_count").GetInt32());
    }

    [Fact]
    public async Task Reading_needs_message_read_and_replying_needs_message_write()
    {
        var customer = await CustomerWithThreadAsync();
        var threadId = await ThreadIdAsync(customer.CustomerId);

        using var notifier = Staff(null, Scopes.Notify.Send, Scopes.Notify.Read, Scopes.Customer.Read);
        var list = await notifier.GetAsync("/staff/threads", Token);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("insufficient_scope", ErrorCode(await list.Content.ReadFromJsonAsync<JsonElement>(Token)));

        using var reader = Staff(null, Scopes.Message.Read);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/staff/threads", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReplyAsync(reader, threadId, "สวัสดี")).StatusCode);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/threads", Token)).StatusCode);
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
    }

    [Fact]
    public async Task Branch_bound_staff_see_and_answer_only_their_branches_threads_and_unfiled_ones()
    {
        var mine = await CustomerWithThreadAsync();
        var theirs = await CustomerWithThreadAsync();
        var unfiled = await CustomerWithThreadAsync();
        var mineThread = await ThreadIdAsync(mine.CustomerId);
        var theirThread = await ThreadIdAsync(theirs.CustomerId);
        await FileUnderBranchAsync(mineThread, "brn_mine_threads");
        await FileUnderBranchAsync(theirThread, "brn_other_threads");

        using var client = Staff(["brn_mine_threads"]);

        Assert.NotNull(await ThreadOfAsync(client, mine.CustomerId));
        Assert.NotNull(await ThreadOfAsync(client, unfiled.CustomerId));
        Assert.Null(await ThreadOfAsync(client, theirs.CustomerId));

        var refused = await ReplyAsync(client, theirThread, "สวัสดี");
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("forbidden", ErrorCode(await refused.Content.ReadFromJsonAsync<JsonElement>(Token)));
        Assert.Empty(fixture.Line.PushesTo(theirs.LineUserId!));

        Assert.Equal(HttpStatusCode.Created, (await ReplyAsync(client, mineThread, "สวัสดี")).StatusCode);

        using var everyone = Staff();
        Assert.NotNull(await ThreadOfAsync(everyone, theirs.CustomerId));
    }

    [Fact]
    public async Task A_thread_is_filed_under_the_branch_the_customer_last_booked_at()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var lineUserId = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var startsAt = clinic.ShiftStartsAt.AddHours(3);
            db.Set<Appointment>().Add(Appointment.Book(clinic.BranchId, clinic.CustomerId, clinic.PetId, clinic.ServiceId, clinic.StaffId, startsAt, startsAt.AddMinutes(30)));
            await db.SaveChangesAsync(Token);
        }

        await TextAsync(lineUserId, "จะเข้าไปตามนัดค่ะ");

        using var client = Staff([clinic.BranchId]);
        var thread = (await ThreadOfAsync(client, clinic.CustomerId))!.Value;
        Assert.Equal(clinic.BranchId, thread.GetProperty("branch_id").GetString());

        using var elsewhere = Staff(["brn_somewhere_else"]);
        Assert.Null(await ThreadOfAsync(elsewhere, clinic.CustomerId));
    }

    [Fact]
    public async Task Threads_awaiting_a_reply_come_first_and_paging_neither_skips_nor_repeats()
    {
        var answered = await CustomerWithThreadAsync();
        using var client = Staff();
        (await ReplyAsync(client, await ThreadIdAsync(answered.CustomerId), "ตอบแล้ว")).EnsureSuccessStatusCode();
        await CustomerWithThreadAsync();
        await CustomerWithThreadAsync();

        var seen = new List<JsonElement>();
        string? cursor = null;

        do
        {
            var page = await client.GetFromJsonAsync<JsonElement>(
                $"/staff/threads?messages=0&limit=2{(cursor is null ? string.Empty : $"&cursor={cursor}")}", Token);
            seen.AddRange(page.GetProperty("data").EnumerateArray());
            cursor = page.GetProperty("page").TryGetProperty("next_cursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
        }
        while (cursor is not null);

        var ids = seen.Select(item => item.GetProperty("id").GetString()).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Contains(seen, item => item.GetProperty("customer_id").GetString() == answered.CustomerId);
        Assert.All(seen, item => Assert.Empty(item.GetProperty("messages").EnumerateArray()));

        var statuses = seen.Select(item => item.GetProperty("status").GetString()).ToList();
        var firstReplied = statuses.IndexOf("replied");
        Assert.True(firstReplied >= 0);
        Assert.DoesNotContain("awaiting_reply", statuses.Skip(firstReplied));

        var awaiting = await client.GetFromJsonAsync<JsonElement>("/staff/threads?status=awaiting_reply&limit=100&messages=0", Token);
        Assert.All(awaiting.GetProperty("data").EnumerateArray(), item => Assert.Equal("awaiting_reply", item.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task Merging_two_records_folds_their_threads_into_one()
    {
        var surviving = await CustomerWithThreadAsync("ข้อความแรก");
        var duplicate = await CustomerWithThreadAsync("ข้อความจากเรกคอร์ดซ้ำ");
        var duplicateThread = await ThreadIdAsync(duplicate.CustomerId);

        // The survivor's LINE address is in the way of nothing; the duplicate's moves only by its record.
        using var counter = Staff(null, Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Customer.Merge, Scopes.Message.Read);
        var merged = await counter.PostAsJsonAsync(
            $"/staff/customers/{surviving.CustomerId}/merge",
            new { duplicate_customer_id = duplicate.CustomerId },
            Token);
        merged.EnsureSuccessStatusCode();
        Assert.Equal(1, (await merged.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("moved").GetProperty("thread_messages").GetInt32());

        var thread = (await ThreadOfAsync(counter, surviving.CustomerId))!.Value;
        Assert.Equal(2, thread.GetProperty("message_count").GetInt32());
        Assert.Equal(2, thread.GetProperty("unread_count").GetInt32());
        Assert.Null(await ThreadOfAsync(counter, duplicate.CustomerId));

        var gone = await counter.GetFromJsonAsync<JsonElement>($"/staff/threads?thread_id={duplicateThread}", Token);
        Assert.Empty(gone.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public void The_reply_endpoint_is_under_the_messaging_rate_limit()
    {
        using var scope = fixture.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        var reply = Assert.Single(endpoints, endpoint =>
            endpoint.RoutePattern.RawText?.TrimStart('/') == "staff/threads/{id}/messages"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true);

        Assert.Equal(RateLimitingExtensions.MessagingPolicy, reply.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
    }
}
