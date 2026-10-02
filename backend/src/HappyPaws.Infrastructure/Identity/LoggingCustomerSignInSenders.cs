using HappyPaws.Application.Identity;
using HappyPaws.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Identity;

/// <summary><c>Auth:SignIn:*</c> — where reset links point, and whether undelivered secrets may be logged.</summary>
public sealed class SignInDeliveryOptions
{
    public const string SectionName = "Auth:SignIn";

    /// <summary>The frontend's reset page. The token goes in the fragment, which never reaches a server log.</summary>
    public string PasswordResetUrlTemplate { get; init; } = "http://localhost:3000/reset-password#token={token}";

    /// <summary>
    /// Development only. Writes one-time codes and reset links to the log, because there is no SMS
    /// or email adapter to deliver them. Both are credentials: never switch this on where logs
    /// leave the machine.
    /// </summary>
    public bool LogUndeliveredSecrets { get; init; }
}

/// <summary>
/// Records that a one-time code would have been texted, until an SMS adapter exists.
/// <para>
/// Loud like the other placeholders, and careful like the invitation sender: the number and the
/// purpose are logged at warning; the code itself only when
/// <see cref="SignInDeliveryOptions.LogUndeliveredSecrets"/> is on, which the development settings
/// do and nothing else should.
/// </para>
/// </summary>
internal sealed class LoggingOtpSender(
    IOptions<SignInDeliveryOptions> options,
    ILogger<LoggingOtpSender> logger) : IOtpSender
{
    public Task SendAsync(
        string phoneDigits,
        string code,
        OtpPurpose purpose,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "One-time code not delivered — no SMS adapter is registered. phone=***{Tail} purpose={Purpose}",
            phoneDigits[^Math.Min(4, phoneDigits.Length)..],
            purpose);

        if (options.Value.LogUndeliveredSecrets)
        {
            logger.LogWarning("Development only. OTP for {Phone}: {Code} (until {ExpiresAt})", phoneDigits, code, expiresAt);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Records that a reset link would have been sent, until an email or SMS adapter exists.</summary>
internal sealed class LoggingPasswordResetSender(
    IOptions<SignInDeliveryOptions> options,
    ILogger<LoggingPasswordResetSender> logger) : IPasswordResetSender
{
    public Task SendAsync(PasswordResetMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Password reset link not delivered — no {Channel} adapter is registered. user={UserId}",
            message.Email is null ? "SMS" : "email",
            message.UserId);

        if (options.Value.LogUndeliveredSecrets)
        {
            logger.LogWarning(
                "Development only. Password reset link for {UserId}: {Link}",
                message.UserId,
                options.Value.PasswordResetUrlTemplate.Replace("{token}", Uri.EscapeDataString(message.Token), StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }
}
