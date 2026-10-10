using HappyPaws.Application.Reviews;
using HappyPaws.Domain.Reviews;
using HappyPaws.Infrastructure.Persistence.Configurations.Reviews;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Reviews;

/// <summary>Review invitations (<c>docs/contract-extensions.md § 36</c>).</summary>
internal sealed class ReviewInvitationRepository(HappyPawsDbContext db) : IReviewInvitationRepository
{
    /// <summary>PostgreSQL <c>unique_violation</c>.</summary>
    private const string UniqueViolation = "23505";

    public Task<ReviewInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.Set<ReviewInvitation>().FirstOrDefaultAsync(invitation => invitation.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryAddAsync(ReviewInvitation invitation, CancellationToken cancellationToken = default)
    {
        var entry = db.Set<ReviewInvitation>().Add(invitation);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: UniqueViolation,
            ConstraintName: ReviewInvitationConfiguration.VisitIndex,
        })
        {
            // Another rule or another instance invited this visit first. Forget the row, so nothing
            // later in this scope tries to save it again.
            entry.State = EntityState.Detached;
            return false;
        }
    }
}
