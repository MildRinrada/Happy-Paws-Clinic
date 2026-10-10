using HappyPaws.Application.Reviews;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Reviews;

/// <summary><c>Reviews:*</c>: where a review invitation's link points (<c>docs/contract-extensions.md § 36</c>).</summary>
public sealed class ReviewLinkOptions
{
    public const string SectionName = "Reviews";

    /// <summary>The portal's review page, sent to a customer with a portal account.</summary>
    public string PortalUrl { get; init; } = "http://localhost:3000/portal/reviews";

    /// <summary>The public invitation page; <c>{token}</c> is replaced by the one-time token.</summary>
    public string InvitationUrlTemplate { get; init; } = "http://localhost:3000/review/{token}";
}

/// <summary>The review links, from <see cref="ReviewLinkOptions"/>.</summary>
internal sealed class ReviewLinks(IOptions<ReviewLinkOptions> options) : IReviewLinks
{
    public string Portal => options.Value.PortalUrl;

    public string ForToken(string token) =>
        options.Value.InvitationUrlTemplate.Replace("{token}", Uri.EscapeDataString(token), StringComparison.Ordinal);
}
