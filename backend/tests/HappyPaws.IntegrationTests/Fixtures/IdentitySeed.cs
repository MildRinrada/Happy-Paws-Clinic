using HappyPaws.Application.Identity;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Creates a staff account with a role and membership, using the production factories and hasher
/// so the tests exercise the same objects the application does.
/// </summary>
public sealed record SeededStaff(
    string UserId,
    string Email,
    string Password,
    string StaffId,
    string? TotpSecret);

public static class IdentitySeed
{
    public static async Task<SeededStaff> StaffAsync(
        ApiFixture fixture,
        IReadOnlyCollection<string> scopes,
        bool withTwoFactor,
        IReadOnlyCollection<string>? branches = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Unique per call: these tests share one container, so a fixed address would collide on
        // the unique email index the moment two of them run.
        var email = $"vet-{Guid.NewGuid():N}@happypaws.test";
        const string password = "correct-horse-battery";

        var role = Role.Create($"role-{Guid.NewGuid():N}", scopes);
        var account = UserAccount.CreateStaff("สพ.ญ. มินท์ ศรีสุข", email, hasher.Hash(password));

        string? secret = null;
        if (withTwoFactor)
        {
            secret = new TotpService().Enrol(email).Secret;
            account.EnrolTwoFactor(TwoFactorMethod.Totp, secret);
        }

        var membership = StaffMembership.Create(
            account.Id,
            staffType: "veterinarian",
            roleIds: [role.Id],
            branchIds: branches ?? ["brn_sukhumvit39"],
            licenseNo: "ส.พ. 06120");

        db.Set<Role>().Add(role);
        db.Set<UserAccount>().Add(account);
        db.Set<StaffMembership>().Add(membership);

        await db.SaveChangesAsync(cancellationToken);

        return new SeededStaff(account.Id, email, password, membership.Id, secret);
    }

    /// <summary>Reads an account back, bypassing the change tracker used by the request under test.</summary>
    public static async Task<UserAccount?> ReloadAccountAsync(
        ApiFixture fixture,
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<UserAccount>().AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == userId, cancellationToken);
    }

    public static async Task<IReadOnlyList<UserSession>> SessionsAsync(
        ApiFixture fixture,
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<UserSession>().AsNoTracking()
            .Where(session => session.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
