using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Jobs;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.Infrastructure.Persistence.Idempotency;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Automation;

/// <summary>The housekeeping job: what it removes, and what it must leave alone.</summary>
[Collection(nameof(JobsCollection))]
public class HousekeepingTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Long_expired_holds_and_keys_go_while_live_ones_and_holds_that_became_bookings_stay()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var days = now.AddDays(-3);

        var abandoned = SlotHold.Create(clinic.BranchId, clinic.ServiceId, clinic.StaffId, clinic.CustomerId, days.AddHours(1), days.AddHours(2), days);
        var booked = SlotHold.Create(clinic.BranchId, clinic.ServiceId, clinic.StaffId, clinic.CustomerId, days.AddHours(3), days.AddHours(4), days);
        booked.Consume(clinic.CustomerId, days.AddMinutes(1));
        var live = SlotHold.Create(clinic.BranchId, clinic.ServiceId, clinic.StaffId, clinic.CustomerId, now.AddDays(1), now.AddDays(1).AddHours(1), now);
        var justExpired = SlotHold.Create(clinic.BranchId, clinic.ServiceId, clinic.StaffId, clinic.CustomerId, now.AddHours(5), now.AddHours(6), now.AddHours(-1));

        var oldKey = Key(now.AddDays(-3));
        var freshKey = Key(now.AddHours(-1));

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            db.Set<SlotHold>().AddRange(abandoned, booked, live, justExpired);
            db.Set<IdempotencyRecord>().AddRange(oldKey, freshKey);
            await db.SaveChangesAsync(Token);
        }

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<HousekeepingJob>().RunAsync(Token);
        }

        using var check = fixture.CreateScope();
        var after = check.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var holds = await after.Set<SlotHold>().AsNoTracking()
            .Where(hold => hold.BranchId == clinic.BranchId)
            .Select(hold => hold.Id)
            .ToListAsync(Token);
        var keys = await after.Set<IdempotencyRecord>().AsNoTracking()
            .Where(record => record.Key == oldKey.Key || record.Key == freshKey.Key)
            .Select(record => record.Key)
            .ToListAsync(Token);

        Assert.DoesNotContain(abandoned.Id, holds);
        Assert.Contains(booked.Id, holds);
        Assert.Contains(live.Id, holds);
        Assert.Contains(justExpired.Id, holds);
        Assert.Equal(new[] { freshKey.Key }, keys);
    }

    [Fact]
    public async Task LINE_followers_nobody_linked_go_after_fourteen_days_and_linked_or_recent_ones_stay()
    {
        var now = DateTimeOffset.UtcNow;
        var old = LineFollower.Record($"U{Guid.NewGuid():N}", now.AddDays(-15));
        var oldButLinked = LineFollower.Record($"U{Guid.NewGuid():N}", now.AddDays(-30));
        var recent = LineFollower.Record($"U{Guid.NewGuid():N}", now.AddDays(-13));
        var customer = Customer.Create("คุณลูกค้า LINE เก่า", $"08{Random.Shared.Next(10_000_000, 99_999_999)}");
        customer.SetLineAddress(oldButLinked.LineUserId);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            db.Set<LineFollower>().AddRange(old, oldButLinked, recent);
            db.Set<Customer>().Add(customer);
            await db.SaveChangesAsync(Token);
        }

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<HousekeepingJob>().RunAsync(Token);
        }

        using var check = fixture.CreateScope();
        var kept = await check.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<LineFollower>().AsNoTracking()
            .Where(follower => new[] { old.Id, oldButLinked.Id, recent.Id }.Contains(follower.Id))
            .Select(follower => follower.Id)
            .ToListAsync(Token);

        Assert.DoesNotContain(old.Id, kept);
        Assert.Contains(oldButLinked.Id, kept);
        Assert.Contains(recent.Id, kept);
    }

    private static IdempotencyRecord Key(DateTimeOffset createdAt) => new()
    {
        Key = $"key-{Guid.NewGuid():N}",
        Endpoint = "POST /test",
        Subject = "usr_test",
        RequestHash = "hash",
        CreatedAt = createdAt,
        ExpiresAt = createdAt.AddHours(24),
    };
}
