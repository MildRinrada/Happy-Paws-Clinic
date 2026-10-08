using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Notifications.Line;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>docs/contract-extensions.md § 18</c>: the webhook keeps a short list of recent followers, the
/// counter picks one as a customer's LINE address, and every way that pick could go wrong is refused.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class LineFollowerTests(DeliveryFixture fixture) : IAsyncLifetime
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

    private static string NewUserId() => $"U{Guid.NewGuid():N}";

    private async Task WebhookAsync(string type, string userId)
    {
        var body = JsonSerializer.Serialize(new
        {
            destination = "Ubot",
            events = new[] { new { type, mode = "active", replyToken = Guid.NewGuid().ToString("N"), source = new { type = "user", userId } } },
        });

        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/line")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Line-Signature", LineSignature.Compute(NotificationScenario.ChannelSecret, Encoding.UTF8.GetBytes(body)));

        (await client.SendAsync(request, Token)).EnsureSuccessStatusCode();
    }

    private HttpClient Counter(params string[] scopes) =>
        fixture.Notifier(scopes.Length == 0 ? [Scopes.Customer.Read, Scopes.Customer.Write] : scopes);

    /// <summary>The unlinked followers whose LINE name contains the end of this id: the ones a test made.</summary>
    private async Task<JsonElement[]> FollowersAsync(HttpClient client, string userId, bool unlinked = true)
    {
        var q = Uri.EscapeDataString(FakeLineServer.DisplayNameOf(userId));
        var page = await client.GetFromJsonAsync<JsonElement>($"/staff/integrations/line/followers?unlinked={unlinked.ToString().ToLowerInvariant()}&q={q}", Token);
        return [.. page.GetProperty("data").EnumerateArray()];
    }

    private static Task<HttpResponseMessage> LinkAsync(HttpClient client, string customerId, string lineUserId, uint? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/staff/customers/{customerId}/line")
        {
            Content = JsonContent.Create(new { line_user_id = lineUserId }),
        };

        if (version is { } value)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{value}\"");
        }

        return client.SendAsync(request, Token);
    }

    private async Task<LineFollower?> FollowerRowAsync(string lineUserId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<LineFollower>().AsNoTracking().FirstOrDefaultAsync(item => item.LineUserId == lineUserId, Token);
    }

    private async Task<bool> AuditedAsync(string action, string customerId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking().AnyAsync(entry => entry.Action == action && entry.ResourceId == customerId, Token);
    }

    private static string? ErrorField(JsonElement body) =>
        body.GetProperty("error").GetProperty("details").GetProperty("errors").EnumerateObject().FirstOrDefault().Name;

    [Fact]
    public async Task A_follow_is_recorded_with_the_LINE_name_and_picture_and_listed_for_the_counter()
    {
        var userId = NewUserId();

        await WebhookAsync("follow", userId);

        var row = await FollowerRowAsync(userId);
        Assert.NotNull(row);
        Assert.Equal(FakeLineServer.DisplayNameOf(userId), row!.DisplayName);
        Assert.Equal($"https://profile.line-scdn.net/{userId}", row.PictureUrl);
        Assert.Contains(fixture.Line.ProfileReads, call => call.Path.EndsWith(userId, StringComparison.Ordinal));

        using var client = Counter();
        var listed = Assert.Single(await FollowersAsync(client, userId));
        Assert.Equal(userId, listed.GetProperty("line_user_id").GetString());
        Assert.Equal(FakeLineServer.DisplayNameOf(userId), listed.GetProperty("display_name").GetString());
        Assert.True(listed.TryGetProperty("followed_at", out _));
        Assert.False(listed.TryGetProperty("linked_customer_id", out _));
    }

    [Fact]
    public async Task An_unfollow_hides_the_follower_and_drops_their_name_and_picture()
    {
        var userId = NewUserId();
        await WebhookAsync("follow", userId);

        await WebhookAsync("unfollow", userId);

        var row = await FollowerRowAsync(userId);
        Assert.NotNull(row!.UnfollowedAt);
        Assert.Null(row.DisplayName);
        Assert.Null(row.PictureUrl);

        using var client = Counter();
        var page = await client.GetFromJsonAsync<JsonElement>("/staff/integrations/line/followers?unlinked=false", Token);
        Assert.DoesNotContain(page.GetProperty("data").EnumerateArray(), item => item.GetProperty("line_user_id").GetString() == userId);

        // Following again makes them pickable once more.
        await WebhookAsync("follow", userId);
        Assert.Single(await FollowersAsync(client, userId));
    }

    [Fact]
    public async Task A_message_from_an_earlier_follower_puts_them_on_the_list()
    {
        var userId = NewUserId();

        await WebhookAsync("message", userId);

        var row = await FollowerRowAsync(userId);
        Assert.NotNull(row);
        Assert.Equal(FakeLineServer.DisplayNameOf(userId), row!.DisplayName);

        await WebhookAsync("message", userId);
        var again = await FollowerRowAsync(userId);
        Assert.Equal(row.Id, again!.Id);
        Assert.True(again.LastSeenAt >= row.LastSeenAt);
    }

    [Fact]
    public async Task The_list_and_the_link_need_customer_write()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        using var reader = Counter(Scopes.Customer.Read, Scopes.Integration.Manage, Scopes.Notify.Send);

        var list = await reader.GetAsync("/staff/integrations/line/followers", Token);
        var link = await LinkAsync(reader, customer.CustomerId, NewUserId());
        var unlink = await reader.DeleteAsync($"/staff/customers/{customer.CustomerId}/line", Token);

        Assert.All(new[] { list, link, unlink }, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        var body = await list.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/integrations/line/followers", Token)).StatusCode);
    }

    [Fact]
    public async Task Linking_a_follower_makes_LINE_the_customers_channel_and_is_audited()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var userId = NewUserId();
        await WebhookAsync("follow", userId);
        using var client = Counter();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/staff/customers/{customer.CustomerId}", Token);
        Assert.False(detail.GetProperty("line").GetProperty("linked").GetBoolean());
        var version = detail.GetProperty("customer").GetProperty("version").GetUInt32();

        var response = await LinkAsync(client, customer.CustomerId, userId, version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var line = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(line.GetProperty("linked").GetBoolean());
        Assert.True(line.GetProperty("can_receive").GetBoolean());
        Assert.Equal(FakeLineServer.DisplayNameOf(userId), line.GetProperty("display_name").GetString());
        Assert.NotEqual(version, line.GetProperty("version").GetUInt32());
        Assert.NotNull(response.Headers.ETag);
        Assert.True(await AuditedAsync(AuditActions.CustomerLineLinked, customer.CustomerId));

        // The page shows it, the picker no longer offers it, and the full list says whose it is.
        detail = await client.GetFromJsonAsync<JsonElement>($"/staff/customers/{customer.CustomerId}", Token);
        Assert.Equal(FakeLineServer.DisplayNameOf(userId), detail.GetProperty("line").GetProperty("display_name").GetString());
        Assert.Empty(await FollowersAsync(client, userId));
        var all = Assert.Single(await FollowersAsync(client, userId, unlinked: false));
        Assert.Equal(customer.CustomerId, all.GetProperty("linked_customer_id").GetString());

        // A message to the customer now goes on LINE.
        using var sender = fixture.Notifier(Scopes.Notify.Send);
        var message = new { template = "clinic.message", customer_ids = new[] { customer.CustomerId }, variables = new { message = "ทดสอบ" } };
        (await sender.PostAsJsonAsync("/staff/notifications/send", message, Token)).EnsureSuccessStatusCode();
        Assert.Single(fixture.Line.PushesTo(userId));

        // Saving the same address again changes nothing.
        var again = await LinkAsync(client, customer.CustomerId, userId);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(
            line.GetProperty("version").GetUInt32(),
            (await again.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("version").GetUInt32());
    }

    [Fact]
    public async Task An_address_on_another_customer_is_refused_and_names_that_customer()
    {
        var holder = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var other = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var userId = NewUserId();
        await WebhookAsync("follow", userId);
        using var client = Counter();
        (await LinkAsync(client, holder.CustomerId, userId)).EnsureSuccessStatusCode();

        var response = await LinkAsync(client, other.CustomerId, userId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("line_address_taken", error.GetProperty("code").GetString());
        Assert.Equal(holder.CustomerId, error.GetProperty("details").GetProperty("customer_id").GetString());
        Assert.Null((await NotificationScenario.ReloadCustomerAsync(fixture, other.CustomerId, Token)).LineUserId);
    }

    [Fact]
    public async Task Addresses_not_on_the_follower_list_or_blocked_are_refused()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        using var client = Counter();

        var unknown = await LinkAsync(client, customer.CustomerId, NewUserId());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        Assert.Equal("line_user_id", ErrorField(await unknown.Content.ReadFromJsonAsync<JsonElement>(Token)));

        var blocked = NewUserId();
        await WebhookAsync("follow", blocked);
        await WebhookAsync("unfollow", blocked);
        var refused = await LinkAsync(client, customer.CustomerId, blocked);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        var empty = await client.PutAsJsonAsync($"/staff/customers/{customer.CustomerId}/line", new { }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);

        var missing = await LinkAsync(client, "cus_nope", NewUserId());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        Assert.Null((await NotificationScenario.ReloadCustomerAsync(fixture, customer.CustomerId, Token)).LineUserId);
    }

    [Fact]
    public async Task A_stale_version_is_a_conflict()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var userId = NewUserId();
        await WebhookAsync("follow", userId);
        using var client = Counter();
        var version = (await client.GetFromJsonAsync<JsonElement>($"/staff/customers/{customer.CustomerId}", Token))
            .GetProperty("customer").GetProperty("version").GetUInt32();

        var response = await LinkAsync(client, customer.CustomerId, userId, version + 1);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("version_conflict", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unlinking_frees_the_address_and_messages_go_by_SMS()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var userId = NewUserId();
        await WebhookAsync("follow", userId);
        using var client = Counter();
        (await LinkAsync(client, customer.CustomerId, userId)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/staff/customers/{customer.CustomerId}/line", Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null((await NotificationScenario.ReloadCustomerAsync(fixture, customer.CustomerId, Token)).LineUserId);
        Assert.True(await AuditedAsync(AuditActions.CustomerLineUnlinked, customer.CustomerId));
        Assert.Single(await FollowersAsync(client, userId));

        using var sender = fixture.Notifier(Scopes.Notify.Send);
        var message = new { template = "clinic.message", customer_ids = new[] { customer.CustomerId }, variables = new { message = "ทดสอบ" } };
        (await sender.PostAsJsonAsync("/staff/notifications/send", message, Token)).EnsureSuccessStatusCode();
        Assert.Empty(fixture.Line.PushesTo(userId));
        Assert.Single(fixture.Transports.SmsTo(customer.PhoneDigits));

        // Nothing left to remove is not an error.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/staff/customers/{customer.CustomerId}/line", Token)).StatusCode);
    }

    [Fact]
    public async Task A_linked_customer_who_blocks_the_account_is_marked_like_any_other()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        var userId = NewUserId();
        await WebhookAsync("follow", userId);
        using var client = Counter();
        (await LinkAsync(client, customer.CustomerId, userId)).EnsureSuccessStatusCode();

        await WebhookAsync("unfollow", userId);

        var line = (await client.GetFromJsonAsync<JsonElement>($"/staff/customers/{customer.CustomerId}", Token)).GetProperty("line");
        Assert.True(line.GetProperty("linked").GetBoolean());
        Assert.False(line.GetProperty("can_receive").GetBoolean());
        Assert.True(line.TryGetProperty("unfollowed_at", out _));

        await WebhookAsync("follow", userId);
        line = (await client.GetFromJsonAsync<JsonElement>($"/staff/customers/{customer.CustomerId}", Token)).GetProperty("line");
        Assert.True(line.GetProperty("can_receive").GetBoolean());
    }
}
