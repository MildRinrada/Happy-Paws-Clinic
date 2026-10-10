namespace HappyPaws.Domain.PrivacyPolicies;

/// <summary>
/// One numbered part of the clinic's privacy notice (<c>docs/contract-extensions.md § 35</c>).
/// </summary>
/// <param name="Anchor">
/// The part's address on the page, <c>/privacy#cookies</c>, and the table of contents' target.
/// Lowercase letters, digits and single hyphens, starting with a letter.
/// </param>
/// <param name="Heading">One line, as readers see it.</param>
/// <param name="Body">
/// Plain text in the small format <see cref="PrivacyPolicyText"/> reads: a blank line starts a new
/// paragraph, a line starting with <c>- </c> is a list item, and <c>[label](/path)</c> links to a
/// page of the clinic's own site. Nothing else is markup.
/// </param>
public sealed record PrivacyPolicySection(string Anchor, string Heading, string Body);
