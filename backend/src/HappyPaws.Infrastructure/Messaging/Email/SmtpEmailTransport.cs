using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HappyPaws.Infrastructure.Messaging.Email;

/// <summary>One email: who it goes to, its subject, and the same text as plain and as HTML.</summary>
internal sealed record EmailMessage(string To, string? ToName, string Subject, string TextBody, string HtmlBody);

/// <summary>Hands an email to a mail server. Throws when the server refuses; callers decide what that means.</summary>
internal interface IEmailTransport
{
    Task SendAsync(EmailMessage message, string fromName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends through the clinic's SMTP server with MailKit.
/// <para>
/// A connection per message. Sign-in codes and invitations are a handful an hour, and a pooled
/// connection that the server has quietly closed is a failure mode a clinic would only find when a
/// customer could not sign in.
/// </para>
/// </summary>
internal sealed class SmtpEmailTransport(IOptions<EmailOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, string fromName, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        // Registered only when both are set (EmailOptions.IsConfigured); a missing one is a wiring bug.
        var host = settings.Host ?? throw new InvalidOperationException("Messaging:Email:Host is not set.");
        var from = settings.FromAddress ?? throw new InvalidOperationException("Messaging:Email:FromAddress is not set.");

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName ?? fromName, from));
        mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(host, settings.Port, Security(settings.Security), cancellationToken);

        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static SecureSocketOptions Security(string value) => value.Trim().ToLowerInvariant() switch
    {
        "ssl" => SecureSocketOptions.SslOnConnect,
        "none" => SecureSocketOptions.None,
        _ => SecureSocketOptions.StartTls,
    };
}
