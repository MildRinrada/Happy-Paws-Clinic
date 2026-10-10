using HappyPaws.Domain.Common;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>
/// The honesty rules of a review: pending until staff decide, rejected only for a listed reason,
/// and never rewritten by anyone (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
public class CustomerReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);

    private static CustomerReview Review(int rating = 5, string? text = "หมอใจดีมาก", ReviewNameChoice choice = ReviewNameChoice.FirstName) =>
        CustomerReview.Submit(
            "cus_1", "brn_1", new DateOnly(2026, 10, 8), "inv_1", "pet_1", "สุนัข",
            rating, text, choice, "คุณสมชาย ใจดี", ReviewSource.Portal, invitationId: null, Now);

    [Fact]
    public void A_new_review_is_pending_and_keeps_only_the_first_name()
    {
        var review = Review();

        Assert.StartsWith("rev_", review.Id);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Equal("สมชาย", review.PublicName);
        Assert.Equal("สุนัข", review.PetSpecies);
        Assert.Null(review.PublishedAt);
    }

    [Fact]
    public void An_anonymous_review_keeps_no_name()
    {
        Assert.Null(Review(choice: ReviewNameChoice.Anonymous).PublicName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void A_score_outside_one_to_five_is_refused(int rating)
    {
        Assert.Throws<DomainException>(() => Review(rating));
    }

    [Fact]
    public void Markup_is_refused_and_a_score_alone_is_a_review()
    {
        Assert.Throws<DomainException>(() => Review(text: "<b>ดี</b>"));
        Assert.Null(Review(text: "   ").Text);
    }

    [Fact]
    public void Publishing_records_who_and_when_and_happens_once()
    {
        var review = Review();

        review.Publish("stf_1", Now.AddHours(1));

        Assert.Equal(ReviewStatus.Published, review.Status);
        Assert.Equal("stf_1", review.ModeratedByStaffId);
        Assert.Equal(Now.AddHours(1), review.PublishedAt);
        Assert.Throws<StateConflictException>(() => review.Publish("stf_2", Now.AddHours(2)));
    }

    [Fact]
    public void A_published_review_can_be_taken_down_at_the_customers_request()
    {
        var review = Review();
        review.Publish("stf_1", Now);

        review.Reject(ReviewRejectReason.CustomerRequest, "stf_2", Now.AddDays(3));

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal(ReviewRejectReason.CustomerRequest, review.RejectReason);
        Assert.Equal("stf_2", review.ModeratedByStaffId);
    }

    [Fact]
    public void Rejected_is_final()
    {
        var review = Review();
        review.Reject(ReviewRejectReason.Spam, "stf_1", Now);

        Assert.Throws<StateConflictException>(() => review.Publish("stf_1", Now));
        Assert.Throws<StateConflictException>(() => review.Reject(ReviewRejectReason.Offensive, "stf_1", Now));
        Assert.Throws<StateConflictException>(() => review.Reply("ขอบคุณ", "stf_1", Now));
    }

    [Fact]
    public void A_reason_must_come_from_the_list()
    {
        Assert.Throws<DomainException>(() => Review().Reject((ReviewRejectReason)99, "stf_1", Now));
    }

    [Fact]
    public void The_list_of_reasons_is_fixed_and_each_has_a_Thai_label()
    {
        Assert.Equal(
            [ReviewRejectReason.PersonalData, ReviewRejectReason.Offensive, ReviewRejectReason.OffTopic, ReviewRejectReason.Spam, ReviewRejectReason.CustomerRequest],
            Enum.GetValues<ReviewRejectReason>());
        Assert.Equal("ลูกค้าขอให้นำออก", ReviewRejectReasons.Label(ReviewRejectReason.CustomerRequest));
        Assert.All(Enum.GetValues<ReviewRejectReason>(), reason => Assert.False(string.IsNullOrWhiteSpace(ReviewRejectReasons.Label(reason))));
    }

    [Fact]
    public void A_reply_can_be_rewritten_and_never_touches_the_customers_words()
    {
        var review = Review();

        Assert.False(review.Reply("ขอบคุณค่ะ", "stf_1", Now));
        Assert.True(review.Reply("ขอบคุณมากค่ะ แล้วพบกันใหม่", "stf_2", Now.AddMinutes(5)));

        Assert.Equal("ขอบคุณมากค่ะ แล้วพบกันใหม่", review.ReplyText);
        Assert.Equal("stf_2", review.RepliedByStaffId);
        Assert.Equal("หมอใจดีมาก", review.Text);
        Assert.Equal(5, review.Rating);
    }

    [Fact]
    public void Nothing_can_set_the_score_or_the_words_after_they_are_written()
    {
        var writable = typeof(CustomerReview).GetProperties()
            .Where(property => property.Name is nameof(CustomerReview.Text) or nameof(CustomerReview.Rating))
            .Select(property => property.SetMethod)
            .Where(setter => setter is not null && setter.IsPublic);

        Assert.Empty(writable);
    }
}
