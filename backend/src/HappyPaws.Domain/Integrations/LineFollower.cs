using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// Someone who followed the clinic's LINE Official Account, or wrote to it, recently: the list the
/// counter picks a customer's LINE address from. Not in the API reference; see
/// <c>docs/contract-extensions.md § 18</c>.
/// <para>
/// <b>An address book entry, not an identity.</b> LINE gives each Official Account its own user id
/// for each person, and nobody can type it, so the counter needs a way to see the address of the
/// person standing in front of them. This row is that and nothing more: it signs nobody in, links
/// no account, and is never matched to a customer by phone or name. A member of staff picks it,
/// the way they would type an email address (<c>docs/assumptions.md § A</c>).
/// </para>
/// <para>
/// <b>Kept briefly.</b> The display name and picture are personal data of someone who may never
/// become a customer. A row nobody has linked is deleted <see cref="Retention"/> after it was last
/// seen, and an unfollow clears the name and picture at once.
/// </para>
/// </summary>
public sealed class LineFollower : Entity
{
    /// <summary>How long a follower nobody has linked is kept, and how far back the counter's list looks.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>LINE's own limit on a user id is 33 characters; the column allows what customers do.</summary>
    public const int LineUserIdMax = 64;

    public const int DisplayNameMax = 100;

    public const int PictureUrlMax = 500;

    private LineFollower()
    {
    }

    /// <summary>The user id LINE gives this clinic's account for this person.</summary>
    public string LineUserId { get; private set; } = string.Empty;

    /// <summary>The name they show on LINE, from LINE's profile API. Null when it could not be read or after an unfollow.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>Their LINE picture, an <c>https</c> address on LINE's servers. Null as for the name.</summary>
    public string? PictureUrl { get; private set; }

    /// <summary>
    /// When they last followed the account; for someone who followed before rows were kept, when
    /// they were first seen writing to it.
    /// </summary>
    public DateTimeOffset FollowedAt { get; private set; }

    /// <summary>When they last followed or wrote to the account: what the counter's list and the clean-up go by.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Set when they blocked the account. Cleared if they follow again.</summary>
    public DateTimeOffset? UnfollowedAt { get; private set; }

    public bool IsFollowing => UnfollowedAt is null;

    /// <summary>
    /// A new row for someone seen for the first time: on a follow, or on a message from someone who
    /// followed before rows were kept. The profile is set apart with <see cref="UpdateProfile"/>,
    /// since LINE may not answer for it.
    /// </summary>
    public static LineFollower Record(string lineUserId, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(lineUserId) || lineUserId.Trim().Length > LineUserIdMax)
        {
            throw new DomainException("invalid_line_follower", "LINE user id ไม่ถูกต้อง");
        }

        return new LineFollower
        {
            Id = EntityId.New(EntityId.LineFollower),
            LineUserId = lineUserId.Trim(),
            FollowedAt = at,
            LastSeenAt = at,
        };
    }

    /// <summary>Followed again: pickable once more, and back at the top of the list.</summary>
    public void Refollow(DateTimeOffset at)
    {
        FollowedAt = at;
        LastSeenAt = at;
        UnfollowedAt = null;
    }

    /// <summary>They wrote to the account. Moves them back to the top of the counter's list.</summary>
    public void Seen(DateTimeOffset at)
    {
        if (at > LastSeenAt)
        {
            LastSeenAt = at;
        }
    }

    /// <summary>They blocked the account: no longer pickable, and their name and picture are not kept.</summary>
    public void Unfollow(DateTimeOffset at)
    {
        UnfollowedAt ??= at;
        DisplayName = null;
        PictureUrl = null;
    }

    /// <summary>
    /// The name and picture LINE reports. A blank keeps nothing; a picture that is not an
    /// <c>https</c> address is dropped, since the counter's page shows it as an image.
    /// </summary>
    public void UpdateProfile(string? displayName, string? pictureUrl)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        DisplayName = name is { Length: > DisplayNameMax } ? name[..DisplayNameMax] : name;

        PictureUrl = pictureUrl is { Length: > 0 and <= PictureUrlMax }
            && Uri.TryCreate(pictureUrl.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
                ? uri.AbsoluteUri
                : null;
    }
}
