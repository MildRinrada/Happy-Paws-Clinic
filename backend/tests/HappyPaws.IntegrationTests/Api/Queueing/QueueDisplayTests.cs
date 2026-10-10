using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Queueing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// Waiting-room screens and their tokens (<c>docs/contract-extensions.md § 30</c>): who may make
/// one, that each secret is shown once and stored as a hash, that the token opens the display board
/// and its branch's stream and nothing else, and that a revoked one stops.
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueueDisplayTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] BoardFields = ["branch_id", "branch_name", "service_date", "waiting_count", "exam_rooms", "tickets"];

    private static readonly string[] TicketFields = ["number", "status", "room_id", "vet_name", "called_at"];

    private HttpClient QueueStaff(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call] : scopes,
            [clinic.BranchId]));

    private static async Task<JsonElement> CreateDisplayAsync(HttpClient staff, string branchId, string name = "ทีวีห้องรอ")
    {
        var response = await staff.PostAsJsonAsync("/staff/queue/displays", new { name, branch_id = branchId }, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private async Task<HttpResponseMessage> ClaimAsync(string code)
    {
        using var anonymous = fixture.CreateClient();
        return await anonymous.PostAsJsonAsync("/queue/displays/claim", new { code }, Token);
    }

    /// <summary>A display made and set up: its id and its token.</summary>
    private async Task<(SeededClinic Clinic, string DisplayId, string DisplayToken)> ScreenAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var created = await CreateDisplayAsync(staff, clinic.BranchId);
        var claim = await ClaimAsync(created.GetProperty("setup_code").GetString()!);
        Assert.Equal(HttpStatusCode.OK, claim.StatusCode);

        var claimed = await claim.Content.ReadFromJsonAsync<JsonElement>(Token);

        return (clinic, created.GetProperty("id").GetString()!, claimed.GetProperty("token").GetString()!);
    }

    private static async Task<string> IssueTicketAsync(HttpClient staff, SeededClinic clinic, object? extra = null)
    {
        using var request = Idempotent("/staff/queue/tickets", extra ?? new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
        });

        var response = await staff.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;
    }

    private async Task<QueueDisplay> ReloadAsync(string displayId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<QueueDisplay>().AsNoTracking().SingleAsync(display => display.Id == displayId, Token);
    }

    [Fact]
    public async Task Making_a_display_needs_queue_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = QueueStaff(clinic, Scopes.Queue.Read, Scopes.Queue.Call);

        var create = await reader.PostAsJsonAsync("/staff/queue/displays", new { name = "ทีวี", branch_id = clinic.BranchId }, Token);
        var list = await reader.GetAsync("/staff/queue/displays", Token);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task A_display_is_made_only_for_the_callers_own_branch()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync("/staff/queue/displays", new { name = "ทีวี", branch_id = other.BranchId }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_setup_code_is_shown_once_and_only_its_hash_is_kept()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync("/staff/queue/displays", new { name = "ทีวีห้องรอชั้น 1", branch_id = clinic.BranchId }, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var code = created.GetProperty("setup_code").GetString()!;
        var id = created.GetProperty("id").GetString()!;

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", code);
        Assert.Equal("pending_setup", created.GetProperty("status").GetString());
        Assert.False(created.TryGetProperty("token", out _));

        var stored = await ReloadAsync(id);
        Assert.Equal(QueueDisplay.Hash(code.Replace("-", string.Empty, StringComparison.Ordinal)), stored.SetupCodeHash);
        Assert.Null(stored.TokenHash);

        // The list never carries a secret or a hash.
        var list = await (await staff.GetAsync("/staff/queue/displays", Token)).Content.ReadAsStringAsync(Token);
        Assert.Contains(id, list);
        Assert.DoesNotContain(code, list);
        Assert.DoesNotContain("setup_code", list);
        Assert.DoesNotContain("hash", list);
        Assert.DoesNotContain("token", list);
    }

    [Fact]
    public async Task The_code_is_traded_once_for_a_token_stored_only_as_a_hash()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var created = await CreateDisplayAsync(staff, clinic.BranchId);
        var code = created.GetProperty("setup_code").GetString()!;

        // Typed loosely on a TV remote: lower case, a space instead of the dash.
        var first = await ClaimAsync(code.ToLowerInvariant().Replace('-', ' '));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("no-store", first.Headers.CacheControl?.ToString());

        var claimed = await first.Content.ReadFromJsonAsync<JsonElement>(Token);
        var token = claimed.GetProperty("token").GetString()!;
        Assert.StartsWith("hpdsp_", token);
        Assert.Equal(clinic.BranchId, claimed.GetProperty("branch_id").GetString());

        var stored = await ReloadAsync(created.GetProperty("id").GetString()!);
        Assert.Equal(QueueDisplay.Hash(token), stored.TokenHash);
        Assert.NotEqual(token, stored.TokenHash);
        Assert.Null(stored.SetupCodeHash);
        Assert.NotNull(stored.ClaimedAt);

        // Once only.
        var second = await ClaimAsync(code);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        var error = (await second.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("code", error.GetProperty("field").GetString());

        var list = await (await staff.GetAsync($"/staff/queue/displays?branch={clinic.BranchId}", Token)).Content.ReadFromJsonAsync<JsonElement>(Token);
        var row = list.GetProperty("data").EnumerateArray().Single();
        Assert.Equal("active", row.GetProperty("status").GetString());
        Assert.DoesNotContain(token, row.GetRawText());
    }

    [Fact]
    public async Task A_code_past_its_half_hour_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var created = await CreateDisplayAsync(staff, clinic.BranchId);
        var id = created.GetProperty("id").GetString()!;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<QueueDisplay>().Where(display => display.Id == id)
                .ExecuteUpdateAsync(set => set.SetProperty(display => display.SetupExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Token);
        }

        var claim = await ClaimAsync(created.GetProperty("setup_code").GetString()!);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, claim.StatusCode);
    }

    [Fact]
    public async Task The_board_carries_numbers_rooms_and_vets_and_nothing_about_the_patient()
    {
        var (clinic, _, displayToken) = await ScreenAsync();
        using var staff = QueueStaff(clinic);

        var called = await IssueTicketAsync(staff, clinic);
        var waiting = await IssueTicketAsync(staff, clinic, new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
            priority = "emergency",
            priority_reason = "หายใจลำบาก",
        });

        (await staff.PostAsJsonAsync($"/staff/queue/{called}/assign", new { staff_id = clinic.StaffId, room_id = "room_1" }, Token))
            .EnsureSuccessStatusCode();
        (await staff.PostAsync($"/staff/queue/{called}/call", null, Token)).EnsureSuccessStatusCode();

        using var screen = fixture.Authenticated(displayToken);
        var response = await screen.GetAsync("/queue/display-board", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync(Token);
        var board = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(BoardFields.Order(), board.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(clinic.BranchId, board.GetProperty("branch_id").GetString());
        Assert.Equal(1, board.GetProperty("waiting_count").GetInt32());

        var tickets = board.GetProperty("tickets").EnumerateArray().ToList();
        Assert.Equal(2, tickets.Count);
        Assert.All(tickets, ticket => Assert.All(
            ticket.EnumerateObject(), property => Assert.Contains(property.Name, TicketFields)));

        var call = tickets.Single(ticket => ticket.GetProperty("status").GetString() == "called");
        Assert.Equal("room_1", call.GetProperty("room_id").GetString());
        Assert.Equal("สพ.ญ. มินท์ ศรีสุข", call.GetProperty("vet_name").GetString());
        Assert.True(call.TryGetProperty("called_at", out _));

        var next = tickets.Single(ticket => ticket.GetProperty("status").GetString() == "waiting");
        Assert.False(next.TryGetProperty("room_id", out _));

        // No id of a ticket, pet, owner or service; no name of a pet or owner; no urgency or reason.
        foreach (var secret in new[] { called, waiting, clinic.PetId, clinic.CustomerId, clinic.ServiceId, clinic.StaffId,
                     "ลูน่า", "คุณลูกค้า", "emergency", "หายใจลำบาก", "priority" })
        {
            Assert.DoesNotContain(secret, raw);
        }
    }

    [Theory]
    [InlineData("GET", "/staff/queue?branch={branch}")]
    [InlineData("POST", "/staff/queue/tkt_000/call")]
    [InlineData("GET", "/staff/queue/displays")]
    [InlineData("POST", "/staff/queue/displays")]
    [InlineData("GET", "/staff/pets")]
    [InlineData("GET", "/staff/api-keys")]
    [InlineData("GET", "/me")]
    [InlineData("GET", "/portal/home")]
    [InlineData("GET", "/portal/queue/tkt_000")]
    [InlineData("GET", "/appointments/apt_000")]
    public async Task The_token_opens_no_other_route(string method, string path)
    {
        var (clinic, _, displayToken) = await ScreenAsync();
        using var screen = fixture.Authenticated(displayToken);

        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{branch}", clinic.BranchId, StringComparison.Ordinal));
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { name = "x", branch_id = clinic.BranchId });
        }

        var response = await screen.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task On_a_public_route_the_token_counts_for_nothing()
    {
        var (_, _, displayToken) = await ScreenAsync();
        using var screen = fixture.Authenticated(displayToken);

        // A public read answers as it would to anyone; the token is not turned into a caller there.
        var response = await screen.GetAsync("/articles", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_board_is_for_display_tokens_only()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);
        using var owner = fixture.CustomerOf(clinic);
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/queue/display-board", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/queue/display-board", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/queue/display-board", Token)).StatusCode);
    }

    [Fact]
    public async Task Another_branch_is_refused_on_the_board_and_the_stream()
    {
        var (_, _, displayToken) = await ScreenAsync();
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var screen = fixture.Authenticated(displayToken);

        var board = await screen.GetAsync($"/queue/display-board?branch={other.BranchId}", Token);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/queue/stream?branch={other.BranchId}");
        using var stream = await screen.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);

        Assert.Equal(HttpStatusCode.Forbidden, board.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, stream.StatusCode);
    }

    [Fact]
    public async Task A_revoked_token_is_refused_on_its_next_request()
    {
        var (clinic, displayId, displayToken) = await ScreenAsync();
        using var staff = QueueStaff(clinic);
        using var screen = fixture.Authenticated(displayToken);

        Assert.Equal(HttpStatusCode.OK, (await screen.GetAsync("/queue/display-board", Token)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/staff/queue/displays/{displayId}", Token)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await screen.GetAsync("/queue/display-board", Token)).StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/queue/stream?branch={clinic.BranchId}");
        using var stream = await screen.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, stream.StatusCode);

        // Again is still 204, and the list says what happened.
        Assert.Equal(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/staff/queue/displays/{displayId}", Token)).StatusCode);
        var list = await (await staff.GetAsync("/staff/queue/displays", Token)).Content.ReadFromJsonAsync<JsonElement>(Token);
        var row = list.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == displayId);
        Assert.Equal("revoked", row.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_display_at_another_branch_cannot_be_seen_or_revoked()
    {
        var (_, displayId, displayToken) = await ScreenAsync();
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var outsider = QueueStaff(other);

        var list = await (await outsider.GetAsync("/staff/queue/displays", Token)).Content.ReadAsStringAsync(Token);
        var revoke = await outsider.DeleteAsync($"/staff/queue/displays/{displayId}", Token);

        Assert.DoesNotContain(displayId, list);
        Assert.Equal(HttpStatusCode.NotFound, revoke.StatusCode);

        using var screen = fixture.Authenticated(displayToken);
        Assert.Equal(HttpStatusCode.OK, (await screen.GetAsync("/queue/display-board", Token)).StatusCode);
    }

    [Fact]
    public async Task Revoking_before_setup_kills_the_code()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var created = await CreateDisplayAsync(staff, clinic.BranchId);
        await staff.DeleteAsync($"/staff/queue/displays/{created.GetProperty("id").GetString()}", Token);

        var claim = await ClaimAsync(created.GetProperty("setup_code").GetString()!);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, claim.StatusCode);
    }

    [Fact]
    public async Task Making_setting_up_and_revoking_are_audited()
    {
        var (clinic, displayId, _) = await ScreenAsync();
        using var staff = QueueStaff(clinic);
        await staff.DeleteAsync($"/staff/queue/displays/{displayId}", Token);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var actions = await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceId == displayId)
            .ToListAsync(Token);

        var created = Assert.Single(actions, entry => entry.Action == AuditActions.QueueDisplayCreated);
        Assert.Equal(clinic.StaffId, created.ActorStaffId);
        Assert.Equal(clinic.BranchId, created.BranchId);

        var claimed = Assert.Single(actions, entry => entry.Action == AuditActions.QueueDisplayClaimed);
        Assert.Null(claimed.ActorUserId);

        var revoked = Assert.Single(actions, entry => entry.Action == AuditActions.QueueDisplayRevoked);
        Assert.Equal(clinic.StaffId, revoked.ActorStaffId);
    }

    [Fact]
    public async Task The_stream_opens_for_its_branch_and_carries_no_ticket_ids()
    {
        var (clinic, _, displayToken) = await ScreenAsync();
        using var staff = QueueStaff(clinic);
        var ticketId = await IssueTicketAsync(staff, clinic);

        using var screen = fixture.Authenticated(displayToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/queue/stream?branch={clinic.BranchId}");
        using var response = await screen.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(body);

        var frames = new List<(string Event, string Data)>();
        string? current = null;
        var triggered = false;

        while (frames.Count < 2)
        {
            var line = await reader.ReadLineAsync(timeout.Token);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                current = line["event: ".Length..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal) && current is not null)
            {
                frames.Add((current, line["data: ".Length..]));
                current = null;

                if (!triggered)
                {
                    triggered = true;
                    (await staff.PostAsync($"/staff/queue/{ticketId}/call", null, timeout.Token)).EnsureSuccessStatusCode();
                }
            }
        }

        Assert.Equal("board.refresh", frames[0].Event);
        Assert.Equal("ticket.called", frames[1].Event);

        foreach (var (_, data) in frames)
        {
            var payload = JsonDocument.Parse(data).RootElement;
            Assert.Equal(new[] { "branch_id" }, payload.EnumerateObject().Select(property => property.Name));
            Assert.DoesNotContain(ticketId, data);
        }
    }

    [Fact]
    public async Task An_open_stream_ends_at_the_next_heartbeat_after_revocation()
    {
        var (clinic, displayId, displayToken) = await ScreenAsync();
        using var staff = QueueStaff(clinic);
        using var screen = fixture.Authenticated(displayToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/queue/stream?branch={clinic.BranchId}");
        using var response = await screen.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(body);

        // The opening board.refresh, then revoke while the stream is open.
        Assert.Equal("event: board.refresh", await reader.ReadLineAsync(timeout.Token));
        Assert.Equal(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/staff/queue/displays/{displayId}", timeout.Token)).StatusCode);

        // The server closes its side: the read reaches the end instead of the 45-second timeout.
        while (await reader.ReadLineAsync(timeout.Token) is { } line)
        {
            Assert.DoesNotContain("ping", line);
        }
    }
}
