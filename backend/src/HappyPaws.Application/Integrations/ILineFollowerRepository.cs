using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations;

/// <summary>
/// The recent followers of the clinic's LINE account (<c>docs/contract-extensions.md § 18</c>).
/// </summary>
public interface ILineFollowerRepository
{
    void Add(LineFollower follower);

    /// <summary>The row for this LINE address, tracked for an update, or null.</summary>
    Task<LineFollower?> FindTrackedAsync(string lineUserId, CancellationToken cancellationToken = default);

    /// <summary>The same, untracked, for display.</summary>
    Task<LineFollower?> FindAsync(string lineUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Followers still following and seen since <see cref="LineFollowerQuery.SeenSince"/>, most
    /// recently seen first, each with the customer whose record carries their address, if any.
    /// </summary>
    Task<CursorPage<LineFollowerRow>> ListAsync(
        LineFollowerQuery query,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}

/// <param name="SeenSince">The start of the window; nothing older is listed.</param>
/// <param name="UnlinkedOnly">Leave out the addresses already on a customer's record.</param>
/// <param name="Text">Part of the LINE display name.</param>
public sealed record LineFollowerQuery(DateTimeOffset SeenSince, bool UnlinkedOnly, string? Text);

/// <summary>A follower, and the customer their address belongs to.</summary>
public sealed record LineFollowerRow(LineFollower Follower, string? LinkedCustomerId);

/// <summary>A person's LINE profile, as far as LINE tells it.</summary>
public sealed record LineProfile(string? DisplayName, string? PictureUrl);
