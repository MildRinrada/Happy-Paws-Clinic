using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews;

/// <summary>What a customer sends when they write a review, already checked.</summary>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">Their words, or null.</param>
/// <param name="NameChoice">The name the public page shows.</param>
public sealed record ReviewInput(int Rating, string? Text, ReviewNameChoice NameChoice);

/// <summary>
/// The rules every way of writing a review shares (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
public static class ReviewRules
{
    /// <summary>How far back a visit may still be reviewed, in clinic days.</summary>
    public const int WindowDays = 60;

    /// <summary>
    /// Checks a review as sent: <c>rating</c> 1 to 5, <c>text</c> optional and plain, and
    /// <c>display_name</c> <c>first_name</c> or <c>anonymous</c>. Every problem is a
    /// <c>422 validation_failed</c> naming its field.
    /// </summary>
    public static ReviewInput Check(int? rating, string? text, string? displayName)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!ReviewText.IsRating(rating))
        {
            errors["rating"] = [$"ต้องให้คะแนน {ReviewText.MinRating} ถึง {ReviewText.MaxRating} ดาว"];
        }

        if (ReviewText.Problem(text) is { } problem)
        {
            errors["text"] = [problem];
        }

        if (!WireEnum.TryParse<ReviewNameChoice>(displayName, out var choice))
        {
            errors["display_name"] = [$"display_name ต้องเป็น {WireEnum.Names<ReviewNameChoice>()}"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        return new ReviewInput(rating!.Value, text, choice);
    }

    /// <summary>The first day a visit can be reviewed on <paramref name="today"/>.</summary>
    public static DateOnly FirstReviewableDay(DateOnly today) => today.AddDays(-WindowDays);

    /// <summary>The answer when a visit already has a review, or its invitation was used.</summary>
    public static ConflictException AlreadyReviewed() =>
        new(ErrorCodes.ReviewAlreadySubmitted, "การมาครั้งนี้ได้รับรีวิวแล้ว รีวิวหนึ่งครั้งต่อการมาหนึ่งครั้ง")
        {
            MessageEn = "This visit has already been reviewed.",
        };

    /// <summary>The answer when an invitation link is older than its lifetime.</summary>
    public static ConflictException InvitationExpired() =>
        new(ErrorCodes.ReviewInvitationExpired, "ลิงก์รีวิวนี้หมดอายุแล้ว")
        {
            MessageEn = "This review link has expired.",
        };

    /// <summary>Today in the clinic's zone.</summary>
    public static DateOnly ClinicToday(DateTimeOffset now, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
}
