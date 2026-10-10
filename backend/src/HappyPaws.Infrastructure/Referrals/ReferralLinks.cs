using HappyPaws.Application.Referrals;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Referrals;

/// <summary><c>Referrals:*</c>: where a referral link points (<c>docs/contract-extensions.md § 39</c>).</summary>
public sealed class ReferralLinkOptions
{
    public const string SectionName = "Referrals";

    /// <summary>The public page for one code; <c>{code}</c> is replaced by the code.</summary>
    public string LinkTemplate { get; init; } = "http://localhost:3000/r/{code}";
}

/// <summary>The referral link, from <see cref="ReferralLinkOptions"/>.</summary>
internal sealed class ReferralLinks(IOptions<ReferralLinkOptions> options) : IReferralLinks
{
    public string ForCode(string code) =>
        options.Value.LinkTemplate.Replace("{code}", Uri.EscapeDataString(code), StringComparison.Ordinal);
}
