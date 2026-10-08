using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ListLineFollowers;

/// <summary>
/// <c>GET /staff/integrations/line/followers?unlinked=true&amp;q=</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 18</c>.
/// </summary>
/// <param name="Unlinked">True (the default) leaves out addresses already on a customer's record.</param>
/// <param name="Query">Part of the LINE display name.</param>
/// <param name="Page">Paging.</param>
public sealed record ListLineFollowersQuery(bool Unlinked = true, string? Query = null, CursorPageRequest? Page = null);

/// <summary>One follower as the counter's picker shows them.</summary>
/// <param name="LineUserId">The address to send to <c>PUT /staff/customers/{id}/line</c>.</param>
/// <param name="DisplayName">Their LINE name; absent when LINE did not give it.</param>
/// <param name="PictureUrl">Their LINE picture, <c>https</c> on LINE's servers.</param>
/// <param name="FollowedAt">When they followed (or were first seen, for an older follower).</param>
/// <param name="LastSeenAt">When they last followed or wrote: the list's order.</param>
/// <param name="LinkedCustomerId">The customer whose record carries the address, when listing them all.</param>
public sealed record LineFollowerDto(
    string LineUserId,
    string? DisplayName,
    string? PictureUrl,
    DateTimeOffset FollowedAt,
    DateTimeOffset LastSeenAt,
    string? LinkedCustomerId)
{
    public static LineFollowerDto From(LineFollowerRow row) => new(
        row.Follower.LineUserId,
        row.Follower.DisplayName,
        row.Follower.PictureUrl,
        row.Follower.FollowedAt,
        row.Follower.LastSeenAt,
        row.LinkedCustomerId);
}

/// <summary>
/// The people who followed the clinic's LINE account, or wrote to it, in the last
/// <see cref="LineFollower.Retention"/>, most recent first: the counter finds the person in front of
/// them near the top, having just added the account.
/// <para>
/// <b>Short window on purpose.</b> These are names and pictures of people who may never be
/// customers. The list looks back as far as rows are kept and no further, and someone who blocked
/// the account is never listed.
/// </para>
/// </summary>
public sealed class ListLineFollowersHandler(ILineFollowerRepository followers, IClock clock)
{
    public const int QueryMax = 100;

    public async Task<CursorPage<LineFollowerDto>> HandleAsync(
        ListLineFollowersQuery query,
        CancellationToken cancellationToken = default)
    {
        var text = query.Query?.Trim();

        if (text is { Length: > QueryMax })
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["q"] = [$"คำค้นยาวได้ไม่เกิน {QueryMax} ตัวอักษร"],
            });
        }

        var page = await followers.ListAsync(
            new LineFollowerQuery(clock.UtcNow - LineFollower.Retention, query.Unlinked, string.IsNullOrEmpty(text) ? null : text),
            query.Page ?? new CursorPageRequest(),
            cancellationToken);

        return new CursorPage<LineFollowerDto>([.. page.Data.Select(LineFollowerDto.From)], page.NextCursor);
    }
}
