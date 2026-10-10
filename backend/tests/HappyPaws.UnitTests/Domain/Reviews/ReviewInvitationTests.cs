using System.Security.Cryptography;
using System.Text;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>
/// The one-time review link: shown once, kept only as a hash, good for 30 days and one review
/// (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
public class ReviewInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);

    private static (ReviewInvitation Invitation, string? Token) Create(bool withToken = true) =>
        ReviewInvitation.Create("cus_1", "brn_1", new DateOnly(2026, 10, 8), "inv_1", "pet_1", "ตรวจสุขภาพ", withToken, "ard_1", Now);

    [Fact]
    public void Only_the_SHA_256_of_the_token_is_kept()
    {
        var (invitation, token) = Create();

        Assert.NotNull(token);
        Assert.StartsWith(ReviewInvitation.TokenPrefix, token);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token!))), invitation.TokenHash);
        Assert.DoesNotContain(token!, invitation.TokenHash!, StringComparison.Ordinal);
        Assert.True(ReviewInvitation.LooksLikeToken(token));
    }

    [Fact]
    public void Two_invitations_never_share_a_token()
    {
        Assert.NotEqual(Create().Token, Create().Token);
    }

    [Fact]
    public void A_customer_with_a_portal_account_gets_no_token_at_all()
    {
        var (invitation, token) = Create(withToken: false);

        Assert.Null(token);
        Assert.Null(invitation.TokenHash);
    }

    [Fact]
    public void A_link_works_for_thirty_days()
    {
        var (invitation, _) = Create();

        Assert.Equal(Now.AddDays(30), invitation.ExpiresAt);
        Assert.Equal(ReviewInvitationState.Usable, invitation.StateAt(Now.AddDays(29), visitReviewed: false));
        Assert.Equal(ReviewInvitationState.Expired, invitation.StateAt(Now.AddDays(30), visitReviewed: false));
    }

    [Fact]
    public void A_link_is_good_for_one_review()
    {
        var (invitation, _) = Create();

        invitation.MarkUsed("rev_1", Now.AddDays(1));

        Assert.Equal(ReviewInvitationState.Used, invitation.StateAt(Now.AddDays(2), visitReviewed: false));
        Assert.Throws<StateConflictException>(() => invitation.MarkUsed("rev_2", Now.AddDays(2)));
    }

    [Fact]
    public void A_visit_reviewed_in_the_portal_uses_up_its_link()
    {
        var (invitation, _) = Create();

        Assert.Equal(ReviewInvitationState.Used, invitation.StateAt(Now.AddDays(1), visitReviewed: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hprv_short")]
    [InlineData("hpdsp_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("hprv_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/")]
    public void Something_not_shaped_like_a_token_is_not_looked_up(string? presented)
    {
        Assert.False(ReviewInvitation.LooksLikeToken(presented));
    }
}
