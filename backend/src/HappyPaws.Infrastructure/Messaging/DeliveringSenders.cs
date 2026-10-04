using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Identity;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Messaging.Sms;
using HappyPaws.Infrastructure.Staff;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Messaging;

/// <summary>
/// The sign-in and invitation messages, delivered for real.
/// <para>
/// <b>None of them throw.</b> Each is called after its transaction has committed, so the code or
/// the invitation exists whether or not the message arrived; a refused send is logged at error,
/// without the secret, and the person asks again. The log line is what an operator searches when a
/// customer says "the code never came".
/// </para>
/// </summary>
internal static class LinkTemplates
{
    public static string Fill(string template, string token, string? id = null)
    {
        var link = template.Replace("{token}", Uri.EscapeDataString(token), StringComparison.Ordinal);
        return id is null ? link : link.Replace("{id}", Uri.EscapeDataString(id), StringComparison.Ordinal);
    }

    public static string Tail(string phoneDigits) => phoneDigits[^Math.Min(4, phoneDigits.Length)..];
}

/// <summary>One-time codes by SMS.</summary>
internal sealed class SmsOtpSender(
    ISmsTransport sms,
    ClinicNameReader clinic,
    ILogger<SmsOtpSender> logger) : IOtpSender
{
    public async Task SendAsync(
        string phoneDigits,
        string code,
        OtpPurpose purpose,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await sms.SendAsync(phoneDigits, MessageTexts.OtpSms(await clinic.ReadAsync(cancellationToken), code), cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "One-time code not delivered. phone=***{Tail} purpose={Purpose}", LinkTemplates.Tail(phoneDigits), purpose);
        }
    }
}

/// <summary>
/// Password-reset links: by email when the account has an address and email is set up, otherwise
/// by SMS. With neither available the log placeholder records it, as before.
/// </summary>
internal sealed class DeliveringPasswordResetSender(
    IEmailTransport? email,
    ISmsTransport? sms,
    LoggingPasswordResetSender fallback,
    ClinicNameReader clinic,
    IOptions<SignInDeliveryOptions> options,
    ILogger<DeliveringPasswordResetSender> logger) : IPasswordResetSender
{
    public async Task SendAsync(PasswordResetMessage message, CancellationToken cancellationToken = default)
    {
        var link = LinkTemplates.Fill(options.Value.PasswordResetUrlTemplate, message.Token);
        var useEmail = email is not null && message.Email is not null;
        var useSms = !useEmail && sms is not null && message.PhoneDigits is not null;

        if (!useEmail && !useSms)
        {
            await fallback.SendAsync(message, cancellationToken);
            return;
        }

        try
        {
            var clinicName = await clinic.ReadAsync(cancellationToken);

            if (useEmail)
            {
                await email!.SendAsync(MessageTexts.ResetEmail(clinicName, message.Email!, link, message.ExpiresAt), clinicName, cancellationToken);
            }
            else
            {
                await sms!.SendAsync(message.PhoneDigits!, MessageTexts.ResetSms(clinicName, link, message.ExpiresAt), cancellationToken);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Password reset link not delivered by {Channel}. user={UserId}", useEmail ? "email" : "SMS", message.UserId);
        }
    }
}

/// <summary>Staff invitations by email.</summary>
internal sealed class EmailStaffInvitationSender(
    IEmailTransport email,
    ClinicNameReader clinic,
    IOptions<StaffInvitationOptions> options,
    ILogger<EmailStaffInvitationSender> logger) : IStaffInvitationSender
{
    public async Task SendAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default)
    {
        var link = LinkTemplates.Fill(options.Value.AcceptUrlTemplate, message.Token, message.InvitationId);

        try
        {
            var clinicName = await clinic.ReadAsync(cancellationToken);
            await email.SendAsync(
                MessageTexts.InvitationEmail(clinicName, message.Email, message.DisplayName, link, message.ExpiresAt),
                clinicName,
                cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Staff invitation not delivered. invitation={InvitationId}", message.InvitationId);
        }
    }
}
