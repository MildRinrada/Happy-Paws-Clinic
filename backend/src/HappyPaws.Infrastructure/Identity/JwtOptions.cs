using System.ComponentModel.DataAnnotations;

namespace HappyPaws.Infrastructure.Identity;

/// <summary>
/// Signing and validation settings for this clinic instance's access tokens.
/// <para>
/// Each clinic is its own deployment and signs its own tokens, so there is no shared authority to
/// discover — the same key both issues (Infrastructure) and validates (Api).
/// </para>
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    /// <summary>Who issued the token. Conventionally this instance's API host.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Who the token is for.</summary>
    [Required]
    public string Audience { get; init; } = "happypaws-api";

    /// <summary>
    /// HMAC signing key. Must be at least 32 bytes — HS256 with a short key is trivially
    /// brute-forced, and a forged token here is a forged staff identity.
    /// Supply through user-secrets or the environment. Never commit it.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "Auth:Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Contract: access tokens live 15 minutes.</summary>
    [Range(1, 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    /// <summary>
    /// Allowed clock drift when validating expiry. Kept small on purpose: the framework default of
    /// five minutes would extend every 15-minute token by a third.
    /// </summary>
    [Range(0, 120)]
    public int ClockSkewSeconds { get; init; } = 30;
}
