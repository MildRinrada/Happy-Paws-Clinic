using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Common.Concurrency;

/// <summary>
/// Enforces the contract's optimistic concurrency rule: every mutable resource carries a
/// <c>version</c>, the client echoes it in <c>If-Match</c>, and a mismatch answers <c>409</c>.
/// <para>
/// Call this in every use case that mutates an existing resource, immediately after loading it.
/// Two receptionists editing the same appointment is not a rare race in a clinic — it is Tuesday.
/// </para>
/// </summary>
public static class ConcurrencyGuard
{
    /// <summary>
    /// Throws <see cref="VersionConflictException"/> when the caller's <c>If-Match</c> does not
    /// match the loaded entity.
    /// </summary>
    /// <param name="entity">The entity as just loaded from the database.</param>
    /// <param name="ifMatchVersion">
    /// The version from <c>If-Match</c>. When null the check is skipped — see
    /// <see cref="EnsureRequired{TEntity}"/> for endpoints where the header is mandatory.
    /// </param>
    public static void Ensure<TEntity>(TEntity entity, uint? ifMatchVersion)
        where TEntity : Entity
    {
        if (ifMatchVersion is { } expected && expected != entity.Version)
        {
            throw new VersionConflictException(typeof(TEntity).Name, expected, entity.Version);
        }
    }

    /// <summary>
    /// Same check, but a missing <c>If-Match</c> is itself a failure.
    /// <para>
    /// Use this on updates where a blind overwrite would lose clinical or financial data. Making
    /// the header optional everywhere means the protection is only as good as the least careful
    /// client.
    /// </para>
    /// </summary>
    public static void EnsureRequired<TEntity>(TEntity entity, uint? ifMatchVersion)
        where TEntity : Entity
    {
        if (ifMatchVersion is null)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["If-Match"] = ["ต้องส่ง If-Match พร้อม version ของข้อมูลที่อ่านมา"] });
        }

        Ensure(entity, ifMatchVersion);
    }
}
