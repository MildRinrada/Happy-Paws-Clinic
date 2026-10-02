using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// <c>SSE /queue/stream?branch=</c> — the contract's one non-REST surface.
/// <para>
/// Worth testing over real HTTP rather than against the broker directly: the things that break an
/// event stream are the framing, the flush and the response buffering, none of which a unit test
/// of the publisher would notice.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueueStreamTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient QueueStaff(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call] : scopes,
            [clinic.BranchId]));

    /// <summary>How long to keep listening after the triggering action, before concluding nothing else is coming.</summary>
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Opens the stream, fires <paramref name="whileStreaming"/> once the subscription is live, and
    /// reads frames until <paramref name="stop"/> is satisfied or the stream settles.
    /// <para>
    /// The action is deliberately fired from inside the read loop rather than before it. Publishing
    /// before the subscription exists would make every one of these tests a race that passes on a
    /// fast machine and fails on a loaded one.
    /// </para>
    /// <para>
    /// Returns the raw event names, so a test asserts on the wire framing and not only on JSON that
    /// a broken <c>event:</c> line would never have carried.
    /// </para>
    /// </summary>
    private static async Task<List<string>> ReadEventsAsync(
        HttpClient client,
        string branchId,
        Func<List<string>, bool> stop,
        Func<Task> whileStreaming)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/queue/stream?branch={branchId}");

        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);

        var events = new List<string>();
        var triggered = false;

        try
        {
            while (true)
            {
                var line = await reader.ReadLineAsync(timeout.Token);
                if (line is null)
                {
                    break;
                }

                if (line.StartsWith("event: ", StringComparison.Ordinal))
                {
                    events.Add(line["event: ".Length..]);

                    if (stop(events))
                    {
                        break;
                    }
                }

                if (!triggered && events.Count > 0)
                {
                    triggered = true;
                    await whileStreaming();

                    // From here the stream is expected to fall quiet. A test asserting that
                    // something did *not* arrive needs to stop waiting at some point, and this is
                    // that point — everything here is in-process, so three seconds is generous.
                    timeout.CancelAfter(Settle);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Settled or timed out. Whatever arrived is the answer.
        }

        return events;
    }

    private async Task<(SeededClinic Clinic, string TicketId)> TicketAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = QueueStaff(clinic);
        using var request = Idempotent("/staff/queue/tickets", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
        });

        var response = await staff.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return (clinic, ticket.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task The_stream_opens_with_a_board_refresh_so_a_reconnecting_client_re_reads()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var events = await ReadEventsAsync(
            staff, clinic.BranchId, received => received.Count >= 1, () => Task.CompletedTask);

        Assert.Equal("board.refresh", events[0]);
    }

    [Fact]
    public async Task Calling_a_ticket_reaches_a_staff_subscriber_as_ticket_called()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var events = await ReadEventsAsync(
            staff,
            clinic.BranchId,
            received => received.Contains("ticket.called"),
            async () =>
            {
                using var caller = QueueStaff(clinic);
                var response = await caller.PostAsync($"/staff/queue/{ticketId}/call", null, Token);
                response.EnsureSuccessStatusCode();
            });

        Assert.Contains("ticket.called", events);
    }

    [Fact]
    public async Task Skipping_announces_a_board_refresh_because_the_order_changed()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        // The first frame is the opening board.refresh, so the test waits for a second one.
        var events = await ReadEventsAsync(
            staff,
            clinic.BranchId,
            received => received.Count(name => name == "board.refresh") >= 2,
            async () =>
            {
                using var caller = QueueStaff(clinic);
                var response = await caller.PostAsync($"/staff/queue/{ticketId}/skip", null, Token);
                response.EnsureSuccessStatusCode();
            });

        Assert.True(events.Count(name => name == "board.refresh") >= 2);
    }

    [Fact]
    public async Task A_subscriber_at_one_branch_never_sees_another_branchs_events()
    {
        var watched = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (elsewhere, otherTicketId) = await TicketAsync();

        using var staff = QueueStaff(watched);

        var events = await ReadEventsAsync(
            staff,
            watched.BranchId,
            // Never satisfied: this reads until the timeout on purpose, so anything that leaked
            // across branches has every chance to arrive.
            _ => false,
            async () =>
            {
                using var caller = QueueStaff(elsewhere);
                var response = await caller.PostAsync($"/staff/queue/{otherTicketId}/call", null, Token);
                response.EnsureSuccessStatusCode();
            });

        // Only the opening frame for this branch.
        Assert.Equal(["board.refresh"], events);
    }

    [Fact]
    public async Task A_customer_watching_the_stream_sees_only_their_own_tickets()
    {
        // The event id alone would leak the clinic's throughput, so the filter is on the server,
        // not in the client.
        var (clinic, ticketId) = await TicketAsync();
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var stranger = fixture.Authenticated(
            TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        var events = await ReadEventsAsync(
            stranger,
            clinic.BranchId,
            _ => false,
            async () =>
            {
                using var caller = QueueStaff(clinic);
                var response = await caller.PostAsync($"/staff/queue/{ticketId}/call", null, Token);
                response.EnsureSuccessStatusCode();
            });

        Assert.Equal(["board.refresh"], events);
    }

    [Fact]
    public async Task An_owner_does_see_their_own_ticket_being_called()
    {
        var (clinic, ticketId) = await TicketAsync();

        using var owner = fixture.CustomerOf(clinic);

        var events = await ReadEventsAsync(
            owner,
            clinic.BranchId,
            received => received.Contains("ticket.called"),
            async () =>
            {
                using var caller = QueueStaff(clinic);
                var response = await caller.PostAsync($"/staff/queue/{ticketId}/call", null, Token);
                response.EnsureSuccessStatusCode();
            });

        Assert.Contains("ticket.called", events);
    }

    [Fact]
    public async Task The_stream_requires_authentication()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync($"/queue/stream?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_watch_a_branch_they_do_not_work_at()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var elsewhere = fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: [Scopes.Queue.Read], branches: ["brn_somewhere_else"]));

        var response = await elsewhere.GetAsync($"/queue/stream?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_disconnecting_client_does_not_leak_its_subscription()
    {
        // Every closed browser tab would otherwise hold a channel for the life of the process.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var broker = fixture.Services.GetRequiredService<Infrastructure.Realtime.QueueEventBroker>();

        var before = broker.SubscriberCount;

        using (var staff = QueueStaff(clinic))
        {
            await ReadEventsAsync(staff, clinic.BranchId, received => received.Count >= 1, () => Task.CompletedTask);
        }

        // The finally block in the broker runs on disconnect; give the server a moment to notice.
        for (var attempt = 0; attempt < 50 && broker.SubscriberCount > before; attempt++)
        {
            await Task.Delay(100, Token);
        }

        Assert.Equal(before, broker.SubscriberCount);
    }
}
