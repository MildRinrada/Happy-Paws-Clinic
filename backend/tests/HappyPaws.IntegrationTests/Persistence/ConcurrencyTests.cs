using System.Net.Http.Json;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Identity;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Persistence;

/// <summary>
/// The <c>version</c> / <c>If-Match</c> mechanic against real PostgreSQL.
/// <para>
/// This is the one part of the foundation that cannot be tested without a real database: it rides
/// on <c>xmin</c>, a system column PostgreSQL maintains itself. An in-memory provider would report
/// success while the whole optimistic-concurrency story was inert — and two receptionists editing
/// the same appointment is not a rare race in a clinic.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConcurrencyTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<string> SeedRoleAsync()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var role = Role.Create($"concurrency-{Guid.NewGuid():N}", [Scopes.Queue.Read]);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(Token);

        return role.Id;
    }

    [Fact]
    public async Task Version_is_populated_from_the_database_not_left_at_zero()
    {
        var roleId = await SeedRoleAsync();

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var role = await db.Set<Role>().AsNoTracking().SingleAsync(r => r.Id == roleId, Token);

        // A zero here would mean the xmin mapping never took effect and every If-Match check in
        // the system silently compares nothing to nothing.
        Assert.NotEqual(0u, role.Version);
    }

    [Fact]
    public async Task Version_changes_when_the_row_is_updated()
    {
        var roleId = await SeedRoleAsync();

        uint before, after;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var role = await db.Set<Role>().SingleAsync(r => r.Id == roleId, Token);
            before = role.Version;

            role.ReplaceScopes([Scopes.Queue.Read, Scopes.Queue.Call]);
            await db.SaveChangesAsync(Token);
        }

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            after = (await db.Set<Role>().AsNoTracking().SingleAsync(r => r.Id == roleId, Token)).Version;
        }

        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task A_concurrent_update_loses_rather_than_silently_overwriting()
    {
        // Two staff members load the same record, both edit, both save. The second save must fail,
        // not quietly discard the first person's change.
        var roleId = await SeedRoleAsync();

        using var firstScope = fixture.CreateScope();
        using var secondScope = fixture.CreateScope();

        var firstDb = firstScope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var firstCopy = await firstDb.Set<Role>().SingleAsync(r => r.Id == roleId, Token);
        var secondCopy = await secondDb.Set<Role>().SingleAsync(r => r.Id == roleId, Token);

        firstCopy.ReplaceScopes([Scopes.Queue.Call]);
        await firstDb.SaveChangesAsync(Token);

        secondCopy.ReplaceScopes([Scopes.Record.Sign]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondDb.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task The_guard_rejects_a_stale_if_match_before_any_work_is_done()
    {
        var roleId = await SeedRoleAsync();

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var role = await db.Set<Role>().AsNoTracking().SingleAsync(r => r.Id == roleId, Token);

        var exception = Assert.Throws<VersionConflictException>(
            () => ConcurrencyGuard.Ensure(role, ifMatchVersion: role.Version + 1));

        Assert.Equal(role.Version, exception.CurrentVersion);
    }

    [Fact]
    public async Task A_current_if_match_passes()
    {
        var roleId = await SeedRoleAsync();

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var role = await db.Set<Role>().AsNoTracking().SingleAsync(r => r.Id == roleId, Token);

        ConcurrencyGuard.Ensure(role, role.Version);
    }

    [Fact]
    public async Task Timestamps_are_stamped_by_the_interceptor_not_by_each_use_case()
    {
        var roleId = await SeedRoleAsync();

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var role = await db.Set<Role>().SingleAsync(r => r.Id == roleId, Token);

        Assert.NotEqual(default, role.CreatedAt);
        Assert.Null(role.UpdatedAt);

        role.ReplaceScopes([Scopes.Queue.Call]);
        await db.SaveChangesAsync(Token);

        Assert.NotNull(role.UpdatedAt);
    }

    [Fact]
    public async Task Sessions_track_the_previous_token_so_reuse_is_detectable()
    {
        // Guards the fix for a real gap: before the previous-generation hash existed, a replayed
        // refresh token was indistinguishable from a random forgery and the theft went unnoticed.
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, cancellationToken: Token);

        using var client = fixture.CreateClient();
        await client.PostAsJsonAsync("/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);
        await client.PostAsync("/auth/refresh", content: null, Token);

        var session = (await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token)).Single();

        Assert.NotNull(session.PreviousRefreshTokenHash);
        Assert.NotEqual(session.RefreshTokenHash, session.PreviousRefreshTokenHash);
    }
}
