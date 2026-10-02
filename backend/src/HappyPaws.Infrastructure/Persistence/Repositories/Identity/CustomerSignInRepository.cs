using HappyPaws.Application.Identity;
using HappyPaws.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Identity;

internal sealed class CustomerSignInRepository(HappyPawsDbContext db) : ICustomerSignInRepository
{
    public void AddVerification(PhoneVerification verification) => db.Set<PhoneVerification>().Add(verification);

    public Task<PhoneVerification?> LatestUsableAsync(
        string phoneDigits,
        OtpPurpose purpose,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        // The newest code only: asking for a new code is how a person says the old one is gone.
        db.Set<PhoneVerification>()
            .Where(verification => verification.PhoneDigits == phoneDigits
                && verification.Purpose == purpose
                && verification.ConsumedAt == null
                && verification.ExpiresAt > now
                && verification.Attempts < PhoneVerification.MaxAttempts)
            .OrderByDescending(verification => verification.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CodesSentSinceAsync(string phoneDigits, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        db.Set<PhoneVerification>()
            .CountAsync(verification => verification.PhoneDigits == phoneDigits && verification.CreatedAt >= since, cancellationToken);

    public void AddResetRequest(PasswordResetRequest request) => db.Set<PasswordResetRequest>().Add(request);

    public Task<PasswordResetRequest?> FindResetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.Set<PasswordResetRequest>().FirstOrDefaultAsync(request => request.TokenHash == tokenHash, cancellationToken);

    public Task<int> ResetsRequestedSinceAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        db.Set<PasswordResetRequest>()
            .CountAsync(request => request.UserId == userId && request.CreatedAt >= since, cancellationToken);
}
