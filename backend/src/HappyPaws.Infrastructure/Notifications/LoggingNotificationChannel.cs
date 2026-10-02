using HappyPaws.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Notifications;

/// <summary>
/// Records what would have been sent, until the real channel adapters exist.
/// <para>
/// A placeholder, and a loud one on purpose. It logs at warning with the customer and template so
/// that a queue call which never reached anyone is visible in the log rather than silently
/// successful — the failure mode that makes a stub dangerous is the one where nobody notices it is
/// still installed.
/// </para>
/// <para>
/// It does not throw. Replace this registration when
/// <c>Infrastructure/Notifications/{Line,Sms,Email}/</c> land; nothing above it changes, because
/// everything above it talks to <see cref="INotificationDispatcher"/> and reaches a channel only
/// through the consent gate.
/// </para>
/// </summary>
internal sealed class LoggingNotificationChannel(ILogger<LoggingNotificationChannel> logger)
    : INotificationChannel
{
    public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Notification not delivered — no channel adapter is registered. "
            + "template={Template} customer={CustomerId} data={Data}",
            request.TemplateKey,
            request.CustomerId,
            string.Join(", ", request.Data.Select(entry => $"{entry.Key}={entry.Value}")));

        return Task.CompletedTask;
    }
}
