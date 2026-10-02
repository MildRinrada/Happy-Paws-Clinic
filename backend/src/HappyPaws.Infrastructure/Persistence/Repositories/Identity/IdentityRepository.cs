using HappyPaws.Application.Identity;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Identity;

/// <summary>
/// EF Core implementation of the identity port.
/// </summary>
public sealed class IdentityRepository(HappyPawsDbContext db) : IIdentityRepository
{
    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Set<UserAccount>().FirstOrDefaultAsync(account => account.Email == email, cancellationToken);

    public Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Set<UserAccount>().FirstOrDefaultAsync(account => account.Id == userId, cancellationToken);

    public Task<UserAccount?> FindByPhoneAsync(string phoneDigits, CancellationToken cancellationToken = default) =>
        db.Set<UserAccount>().FirstOrDefaultAsync(account => account.PhoneNumber == phoneDigits, cancellationToken);

    public void AddAccount(UserAccount account) => db.Set<UserAccount>().Add(account);

    public Task<StaffMembership?> FindStaffMembershipAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Set<StaffMembership>().FirstOrDefaultAsync(membership => membership.UserId == userId, cancellationToken);

    public Task<string?> FindCustomerIdAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>()
            .AsNoTracking()
            .Where(customer => customer.UserId == userId && customer.MergedIntoCustomerId == null)
            .OrderBy(customer => customer.CreatedAt)
            .Select(customer => customer.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> FindRolesAsync(
        IReadOnlyCollection<string> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await db.Set<Role>()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<UserSession?> FindSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        db.Set<UserSession>().FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public Task<UserSession?> FindSessionByRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        db.Set<UserSession>().FirstOrDefaultAsync(
            session => session.RefreshTokenHash == refreshTokenHash, cancellationToken);

    public async Task<RefreshTokenMatch?> MatchRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var session = await db.Set<UserSession>().FirstOrDefaultAsync(
            candidate => candidate.RefreshTokenHash == refreshTokenHash
                || candidate.PreviousRefreshTokenHash == refreshTokenHash,
            cancellationToken);

        return session is null
            ? null
            : new RefreshTokenMatch(session, session.RefreshTokenHash == refreshTokenHash);
    }

    public async Task<IReadOnlyList<UserSession>> ListActiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await db.Set<UserSession>()
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .OrderByDescending(session => session.LastUsedAt)
            .ToListAsync(cancellationToken);

    public void AddSession(UserSession session) => db.Set<UserSession>().Add(session);

    public async Task RevokeOtherSessionsAsync(
        string userId,
        string exceptSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // A set-based update rather than loading every session: an account can accumulate a lot of
        // devices, and this runs on the password-change path where the whole point is that it
        // takes effect immediately.
        await db.Set<UserSession>()
            .Where(session => session.UserId == userId
                && session.Id != exceptSessionId
                && session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAt, now),
                cancellationToken);
    }
}
