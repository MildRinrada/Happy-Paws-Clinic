using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>docs/contract-extensions.md § 22</c> for API keys: the list that never shows a key, and
/// revoking that stops the key on its very next request.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class ApiKeyManagementTests(IntegrationsFixture fixture)
{
    private static readonly string[] CreatorScopes = [Scopes.Integration.Manage, Scopes.Customer.Read];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listing_and_revoking_need_integration_manage()
    {
        var (clinic, creator, _) = await CreatorAsync();
        var (_, keyId, _) = await KeyAsync(creator);
        using var outsider = fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Customer.Read], [clinic.BranchId]));

        var list = await outsider.GetAsync("/staff/api-keys", Token);
        var revoke = await outsider.DeleteAsync($"/staff/api-keys/{keyId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revoke.StatusCode);
        Assert.Equal("insufficient_scope", await IntegrationsScenario.ErrorCodeAsync(revoke, Token));
    }

    [Fact]
    public async Task The_list_identifies_each_key_and_never_shows_it()
    {
        var (clinic, creator, staff) = await CreatorAsync();
        var (_, keyId, key) = await KeyAsync(creator);

        var response = await creator.GetAsync("/staff/api-keys", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(key, raw, StringComparison.Ordinal);
        // hp_ + ten-character prefix + _ + secret; the secret itself may contain '_'.
        var prefix = key[..(ApiKey.KeyPrefix.Length + ApiKey.PrefixLength)];
        Assert.DoesNotContain(key[(prefix.Length + 1)..], raw, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey.Hash(key), raw, StringComparison.Ordinal);

        var row = Row(JsonDocument.Parse(raw).RootElement, keyId);
        Assert.Equal(prefix, row.GetProperty("prefix").GetString());
        Assert.Equal("เครื่องทดสอบ", row.GetProperty("name").GetString());
        Assert.Equal("active", row.GetProperty("status").GetString());
        Assert.Equal([Scopes.Customer.Read], row.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()!));
        Assert.Equal([clinic.BranchId], row.GetProperty("branch_ids").EnumerateArray().Select(s => s.GetString()!));
        Assert.Equal(staff.StaffId, row.GetProperty("created_by_staff_id").GetString());
        Assert.True(row.TryGetProperty("expires_at", out _));
        Assert.False(row.TryGetProperty("key", out _));
    }

    [Fact]
    public async Task A_revoked_key_is_refused_on_its_very_next_request()
    {
        var (_, creator, _) = await CreatorAsync();
        var (client, keyId, _) = await KeyAsync(creator);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/staff/customers", Token)).StatusCode);

        var revoked = await creator.DeleteAsync($"/staff/api-keys/{keyId}", Token);
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);

        var next = await client.GetAsync("/staff/customers", Token);
        Assert.Equal(HttpStatusCode.Unauthorized, next.StatusCode);
        Assert.Equal("unauthenticated", await IntegrationsScenario.ErrorCodeAsync(next, Token));

        var row = Row(await creator.GetFromJsonAsync<JsonElement>("/staff/api-keys", Token), keyId);
        Assert.Equal("revoked", row.GetProperty("status").GetString());
        Assert.True(row.TryGetProperty("revoked_at", out var revokedAt) && revokedAt.ValueKind == JsonValueKind.String);

        // Again: nothing to do, still 204, and only the first is on record.
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/staff/api-keys/{keyId}", Token)).StatusCode);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .SingleAsync(entry => entry.Action == AuditActions.ApiKeyRevoked && entry.ResourceId == keyId, Token);
        Assert.Contains("\"active\"", audit.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_key_is_not_found_and_an_expired_one_reads_expired()
    {
        var (_, creator, _) = await CreatorAsync();
        var (_, keyId, _) = await KeyAsync(creator);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<ApiKey>().Where(key => key.Id == keyId)
                .ExecuteUpdateAsync(set => set.SetProperty(key => key.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1)), Token);
        }

        Assert.Equal("expired", Row(await creator.GetFromJsonAsync<JsonElement>("/staff/api-keys", Token), keyId).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync("/staff/api-keys/key_nothing", Token)).StatusCode);
    }

    [Fact]
    public async Task A_branch_bound_manager_neither_sees_nor_stops_a_key_for_another_branch()
    {
        var here = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var there = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var both = await IdentitySeed.StaffAsync(fixture, CreatorScopes, withTwoFactor: false, branches: [here.BranchId, there.BranchId], cancellationToken: Token);
        using var creator = fixture.Authenticated(TestTokens.Staff(fixture, both.UserId, both.StaffId, CreatorScopes, [here.BranchId, there.BranchId]));

        var response = await creator.PostAsJsonAsync(
            "/staff/api-keys", new { name = "สาขาอื่น", scopes = new[] { Scopes.Customer.Read }, branch_ids = new[] { there.BranchId } }, Token);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var keyId = created.GetProperty("id").GetString()!;
        using var keyClient = fixture.Authenticated(created.GetProperty("key").GetString()!);

        using var hereOnly = fixture.Integrator(here);

        var list = await hereOnly.GetFromJsonAsync<JsonElement>("/staff/api-keys", Token);
        Assert.DoesNotContain(keyId, list.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, (await hereOnly.DeleteAsync($"/staff/api-keys/{keyId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await keyClient.GetAsync("/staff/customers", Token)).StatusCode);

        // Its maker, who works at both, does see it.
        var mine = await creator.GetFromJsonAsync<JsonElement>("/staff/api-keys", Token);
        Assert.Contains(keyId, mine.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetString()));
    }

    private static JsonElement Row(JsonElement list, string id) =>
        list.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("id").GetString() == id);

    /// <summary>A real account and role: a key is checked against them on every request.</summary>
    private async Task<(SeededClinic Clinic, HttpClient Creator, SeededStaff Staff)> CreatorAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var staff = await IdentitySeed.StaffAsync(fixture, CreatorScopes, withTwoFactor: true, branches: [clinic.BranchId], cancellationToken: Token);
        var client = fixture.Authenticated(TestTokens.Staff(fixture, staff.UserId, staff.StaffId, CreatorScopes, [clinic.BranchId]));

        return (clinic, client, staff);
    }

    private async Task<(HttpClient Client, string KeyId, string Key)> KeyAsync(HttpClient creator)
    {
        var response = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "เครื่องทดสอบ", scopes = new[] { Scopes.Customer.Read } }, Token);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var key = created.GetProperty("key").GetString()!;

        return (fixture.Authenticated(key), created.GetProperty("id").GetString()!, key);
    }
}
