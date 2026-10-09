using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>POST /staff/api-keys</c>, and a key at work: exactly its scopes, within its branches, never
/// past its expiry or its creator's current access, stored as a hash, audited, rate limited.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class ApiKeyTests(IntegrationsFixture fixture)
{
    private static readonly string[] CreatorScopes =
    [
        Scopes.Integration.Manage,
        Scopes.Customer.Read,
        Scopes.Customer.Write,
        Scopes.Appointment.Read,
        Scopes.Record.Read,
    ];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Issuing_a_key_needs_integration_manage()
    {
        var (_, creator) = await CreatorAsync(scopes: [Scopes.Customer.Read]);

        var response = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "x", scopes = new[] { Scopes.Customer.Read } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("insufficient_scope", await IntegrationsScenario.ErrorCodeAsync(response, Token));
    }

    [Theory]
    [InlineData("""{ "name": "x", "scopes": [] }""")]
    [InlineData("""{ "name": "", "scopes": ["customer:read"] }""")]
    [InlineData("""{ "name": "x", "scopes": ["record:read"] }""")]
    [InlineData("""{ "name": "x", "scopes": ["integration:manage"] }""")]
    [InlineData("""{ "name": "x", "scopes": ["customer:*"] }""")]
    [InlineData("""{ "name": "x", "scopes": ["*"] }""")]
    [InlineData("""{ "name": "x", "scopes": ["customer:read"], "expires_at": "2020-01-01T00:00:00Z" }""")]
    [InlineData("""{ "name": "x", "scopes": ["customer:read"], "expires_at": "2099-01-01T00:00:00Z" }""")]
    public async Task Keys_that_could_do_too_much_or_live_forever_are_refused(string body)
    {
        var (_, creator) = await CreatorAsync();

        var response = await creator.PostAsync("/staff/api-keys", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_key_may_not_carry_a_scope_its_creator_does_not_hold()
    {
        var (_, creator) = await CreatorAsync(scopes: [Scopes.Integration.Manage, Scopes.Customer.Read]);

        var response = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "x", scopes = new[] { Scopes.Customer.Write } }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("insufficient_scope", await IntegrationsScenario.ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task A_branch_bound_creator_may_not_name_another_branch()
    {
        var (_, creator) = await CreatorAsync();

        var response = await creator.PostAsJsonAsync(
            "/staff/api-keys",
            new { name = "x", scopes = new[] { Scopes.Customer.Read }, branch_ids = new[] { "brn_elsewhere" } },
            Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_key_is_shown_once_and_only_its_hash_is_stored()
    {
        var (clinic, creator) = await CreatorAsync();

        var response = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "ระบบบัญชี", scopes = new[] { Scopes.Customer.Read } }, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var key = created.GetProperty("key").GetString()!;
        var id = created.GetProperty("id").GetString()!;

        Assert.StartsWith(created.GetProperty("prefix").GetString()! + "_", key, StringComparison.Ordinal);
        Assert.Equal([clinic.BranchId], created.GetProperty("branch_ids").EnumerateArray().Select(branch => branch.GetString()!));

        var expiresAt = created.GetProperty("expires_at").GetDateTimeOffset();
        Assert.InRange(expiresAt - DateTimeOffset.UtcNow, TimeSpan.FromDays(89), TimeSpan.FromDays(91));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var stored = await db.Set<ApiKey>().AsNoTracking().SingleAsync(row => row.Id == id, Token);
        var secretPart = key[(key.LastIndexOf('_') + 1)..];

        Assert.Equal(ApiKey.Hash(key), stored.SecretHash);
        Assert.DoesNotContain(secretPart, stored.SecretHash, StringComparison.Ordinal);

        // Every column of the row, read raw: none of them holds the key.
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT row_to_json(k)::text FROM api_keys k WHERE id = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        var row = (string)(await command.ExecuteScalarAsync(Token))!;
        Assert.DoesNotContain(secretPart, row, StringComparison.Ordinal);
        Assert.Contains(stored.Prefix, row, StringComparison.Ordinal);

        var audit = await db.Set<AuditEntry>().AsNoTracking().SingleAsync(entry => entry.Action == AuditActions.ApiKeyCreated && entry.ResourceId == id, Token);
        Assert.DoesNotContain(secretPart, audit.DetailsJson!, StringComparison.Ordinal);
        Assert.Contains(Scopes.Customer.Read, audit.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_key_does_exactly_what_its_scopes_say()
    {
        var (_, creator) = await CreatorAsync();
        using var client = await KeyClientAsync(creator, [Scopes.Customer.Read]);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/staff/customers?q=ทดสอบ", Token)).StatusCode);

        // Its creator holds customer:write; the key does not.
        var write = await client.PostAsJsonAsync("/staff/customers", new { display_name = "x", phone = "0812345678" }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Equal("insufficient_scope", await IntegrationsScenario.ErrorCodeAsync(write, Token));

        // A key cannot mint keys, whatever its creator holds.
        var mint = await client.PostAsJsonAsync("/staff/api-keys", new { name = "x", scopes = new[] { Scopes.Customer.Read } }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, mint.StatusCode);
    }

    [Fact]
    public async Task A_key_cannot_use_the_routes_that_are_for_people()
    {
        var (_, creator) = await CreatorAsync();
        using var client = await KeyClientAsync(creator, [Scopes.Customer.Read]);

        var me = await client.GetAsync("/me", Token);
        var sessions = await client.GetAsync("/me/sessions", Token);
        var calendar = await client.GetAsync("/staff/integrations/calendar/ics", Token);

        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sessions.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, calendar.StatusCode);
    }

    [Fact]
    public async Task A_key_cannot_sign_its_creator_out_everywhere()
    {
        var (_, creator, staff) = await CreatorWithAccountAsync();
        var (client, _) = await KeyAsync(creator, [Scopes.Customer.Read]);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            db.Set<HappyPaws.Domain.Identity.UserSession>().Add(HappyPaws.Domain.Identity.UserSession.Start(
                staff.UserId, ApiKey.Hash(Guid.NewGuid().ToString()), DateTimeOffset.UtcNow, "browser", "127.0.0.1", twoFactorSatisfied: true));
            await db.SaveChangesAsync(Token);
        }

        await client.PostAsJsonAsync("/auth/logout", new { all_devices = true }, Token);

        var sessions = await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token);
        Assert.NotEmpty(sessions);
        Assert.All(sessions, session => Assert.Null(session.RevokedAt));
    }

    [Fact]
    public async Task A_key_is_audited_as_itself_and_its_use_is_recorded()
    {
        var (_, creator) = await CreatorAsync();
        var (client, keyId) = await KeyAsync(creator, [Scopes.Customer.Write]);

        var customerId = await IntegrationsScenario.CreateCustomerAsync(client, Token);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .SingleAsync(entry => entry.Action == AuditActions.CustomerCreated && entry.ResourceId == customerId, Token);
        Assert.Contains(keyId, audit.DetailsJson!, StringComparison.Ordinal);

        var stored = await db.Set<ApiKey>().AsNoTracking().SingleAsync(row => row.Id == keyId, Token);
        Assert.NotNull(stored.LastUsedAt);
        Assert.True(DateTimeOffset.UtcNow - stored.LastUsedAt!.Value < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task An_expired_or_revoked_key_is_refused()
    {
        var (_, creator) = await CreatorAsync();
        var (expired, expiredId) = await KeyAsync(creator, [Scopes.Customer.Read]);
        var (revoked, revokedId) = await KeyAsync(creator, [Scopes.Customer.Read]);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

            await db.Set<ApiKey>().Where(key => key.Id == expiredId)
                .ExecuteUpdateAsync(set => set.SetProperty(key => key.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Token);
            await db.Set<ApiKey>().Where(key => key.Id == revokedId)
                .ExecuteUpdateAsync(set => set.SetProperty(key => key.RevokedAt, DateTimeOffset.UtcNow), Token);
        }

        var first = await expired.GetAsync("/staff/customers", Token);
        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal("unauthenticated", await IntegrationsScenario.ErrorCodeAsync(first, Token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await revoked.GetAsync("/staff/customers", Token)).StatusCode);
    }

    [Fact]
    public async Task A_made_up_or_altered_key_is_refused()
    {
        var (_, creator) = await CreatorAsync();
        var (genuine, _) = await KeyAsync(creator, [Scopes.Customer.Read]);
        var key = genuine.DefaultRequestHeaders.Authorization!.Parameter!;

        using var altered = fixture.CreateClient();
        altered.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key[..^2] + (key[^2] == 'A' ? "BB" : "AA"));

        using var invented = fixture.CreateClient();
        invented.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "hp_abcdefghij_" + new string('A', 43));

        Assert.Equal(HttpStatusCode.Unauthorized, (await altered.GetAsync("/staff/customers", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await invented.GetAsync("/staff/customers", Token)).StatusCode);
    }

    [Fact]
    public async Task A_key_never_outlives_its_creators_access()
    {
        var (_, creator, staff) = await CreatorWithAccountAsync();
        var (scoped, _) = await KeyAsync(creator, [Scopes.Customer.Read, Scopes.Appointment.Read]);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var membership = await db.Set<StaffMembership>().SingleAsync(row => row.Id == staff.StaffId, Token);
            var role = await db.Set<Role>().SingleAsync(row => row.Id == membership.RoleIds[0], Token);

            role.ReplaceScopes([.. role.Scopes.Where(scope => scope != Scopes.Customer.Read)]);
            await db.SaveChangesAsync(Token);
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await scoped.GetAsync("/staff/customers", Token)).StatusCode);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var membership = await db.Set<StaffMembership>().SingleAsync(row => row.Id == staff.StaffId, Token);

            membership.Deactivate();
            await db.SaveChangesAsync(Token);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await scoped.GetAsync("/staff/appointments", Token)).StatusCode);
    }

    [Fact]
    public async Task A_key_sees_only_its_branches()
    {
        var here = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var there = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var staff = await IdentitySeed.StaffAsync(
            fixture, [Scopes.Integration.Manage, Scopes.Appointment.Read], withTwoFactor: false, branches: [here.BranchId, there.BranchId], cancellationToken: Token);
        using var creator = fixture.Authenticated(TestTokens.Staff(
            fixture, staff.UserId, staff.StaffId, [Scopes.Integration.Manage, Scopes.Appointment.Read], [here.BranchId, there.BranchId]));

        var mine = await BookingScenario.BookAsStaffAsync(fixture, here, 4, Token);
        var theirs = await BookingScenario.BookAsStaffAsync(fixture, there, 4, Token);
        var date = ClinicSeed.ClinicDate(ClinicSeed.SlotAt(4));

        var response = await creator.PostAsJsonAsync(
            "/staff/api-keys",
            new { name = "branch only", scopes = new[] { Scopes.Appointment.Read }, branch_ids = new[] { here.BranchId } },
            Token);
        response.EnsureSuccessStatusCode();
        var key = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("key").GetString()!;
        using var client = fixture.Authenticated(key);

        var ids = await AppointmentIdsAsync(client, date);
        Assert.Contains(mine, ids);
        Assert.DoesNotContain(theirs, ids);

        var creatorIds = await AppointmentIdsAsync(creator, date);
        Assert.Contains(theirs, creatorIds);
    }

    [Fact]
    public async Task A_key_has_its_own_rate_limit()
    {
        var (_, creator) = await CreatorAsync();
        var (client, _) = await KeyAsync(creator, [Scopes.Customer.Read]);

        for (var i = 0; i < IntegrationsFixture.ApiKeyPerMinute; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/staff/customers", Token)).StatusCode);
        }

        var limited = await client.GetAsync("/staff/customers", Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("rate_limited", await IntegrationsScenario.ErrorCodeAsync(limited, Token));

        // The person who made it is counted apart.
        Assert.Equal(HttpStatusCode.OK, (await creator.GetAsync("/staff/customers", Token)).StatusCode);
    }

    private async Task<IReadOnlyList<string>> AppointmentIdsAsync(HttpClient client, string date)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/staff/appointments?date={date}&limit=100", Token);
        return [.. page.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetString()!)];
    }

    private async Task<(SeededClinic Clinic, HttpClient Creator)> CreatorAsync(string[]? scopes = null)
    {
        var (clinic, client, _) = await CreatorWithAccountAsync(scopes);
        return (clinic, client);
    }

    /// <summary>A real account and role in the database: a key is checked against them on every request.</summary>
    private async Task<(SeededClinic Clinic, HttpClient Creator, SeededStaff Staff)> CreatorWithAccountAsync(string[]? scopes = null)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var held = scopes ?? CreatorScopes;

        var staff = await IdentitySeed.StaffAsync(fixture, held, withTwoFactor: true, branches: [clinic.BranchId], cancellationToken: Token);
        var client = fixture.Authenticated(TestTokens.Staff(fixture, staff.UserId, staff.StaffId, held, [clinic.BranchId]));

        return (clinic, client, staff);
    }

    private async Task<HttpClient> KeyClientAsync(HttpClient creator, string[] scopes) => (await KeyAsync(creator, scopes)).Client;

    private async Task<(HttpClient Client, string KeyId)> KeyAsync(HttpClient creator, string[] scopes)
    {
        var response = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "เครื่องทดสอบ", scopes }, Token);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return (fixture.Authenticated(created.GetProperty("key").GetString()!), created.GetProperty("id").GetString()!);
    }
}
