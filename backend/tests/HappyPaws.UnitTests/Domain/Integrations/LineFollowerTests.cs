using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>What a recent LINE follower keeps, and what it lets go of (<c>docs/contract-extensions.md § 18</c>).</summary>
public class LineFollowerTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 8, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_follower_is_following_and_seen_now()
    {
        var follower = LineFollower.Record(" U1234 ", At);

        Assert.StartsWith("lfw_", follower.Id);
        Assert.Equal("U1234", follower.LineUserId);
        Assert.Equal(At, follower.FollowedAt);
        Assert.Equal(At, follower.LastSeenAt);
        Assert.True(follower.IsFollowing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_follower_needs_an_address(string lineUserId)
    {
        Assert.Throws<DomainException>(() => LineFollower.Record(lineUserId, At));
    }

    [Fact]
    public void Only_an_https_picture_is_kept_and_a_long_name_is_cut()
    {
        var follower = LineFollower.Record("U1", At);

        follower.UpdateProfile(new string('ก', 150), "http://example.test/a.png");
        Assert.Equal(LineFollower.DisplayNameMax, follower.DisplayName!.Length);
        Assert.Null(follower.PictureUrl);

        follower.UpdateProfile("  สมใจ  ", "https://profile.line-scdn.net/abc");
        Assert.Equal("สมใจ", follower.DisplayName);
        Assert.Equal("https://profile.line-scdn.net/abc", follower.PictureUrl);

        follower.UpdateProfile(null, "javascript:alert(1)");
        Assert.Null(follower.DisplayName);
        Assert.Null(follower.PictureUrl);
    }

    [Fact]
    public void An_unfollow_drops_the_name_and_picture_and_a_follow_brings_them_back_to_the_list()
    {
        var follower = LineFollower.Record("U1", At);
        follower.UpdateProfile("สมใจ", "https://profile.line-scdn.net/abc");

        follower.Unfollow(At.AddHours(1));

        Assert.False(follower.IsFollowing);
        Assert.Null(follower.DisplayName);
        Assert.Null(follower.PictureUrl);

        follower.Refollow(At.AddHours(2));
        Assert.True(follower.IsFollowing);
        Assert.Equal(At.AddHours(2), follower.FollowedAt);
        Assert.Equal(At.AddHours(2), follower.LastSeenAt);
    }

    [Fact]
    public void A_message_moves_them_up_but_never_back_in_time()
    {
        var follower = LineFollower.Record("U1", At);

        follower.Seen(At.AddMinutes(5));
        Assert.Equal(At.AddMinutes(5), follower.LastSeenAt);
        Assert.Equal(At, follower.FollowedAt);

        follower.Seen(At);
        Assert.Equal(At.AddMinutes(5), follower.LastSeenAt);
    }
}
