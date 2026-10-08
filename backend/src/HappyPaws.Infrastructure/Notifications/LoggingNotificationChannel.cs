using HappyPaws.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Notifications;

/// <summary>
/// Records what would have been sent, when no LINE, SMS or email is set up at all.
/// <para>
/// The fallback <see cref="DeliveringNotificationChannel"/> hands a message to on a machine with
/// no provider configured, which is a developer's. Loud on purpose: it logs at warning with the
/// customer and template so that a queue call which never reached anyone is visible in the log
/// rather than silently successful.
/// </para>
/// <para>
/// It writes the template's data too, which can include a diagnosis. Do not let logs from an
/// instance with no channel configured leave the machine.
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
