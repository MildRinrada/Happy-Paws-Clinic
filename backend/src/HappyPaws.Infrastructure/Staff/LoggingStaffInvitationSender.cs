using HappyPaws.Application.Staff;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Staff;

/// <summary>Where an invitation link points, and whether an undelivered one may be written to the log.</summary>
public sealed class StaffInvitationOptions
{
    public const string SectionName = "Staff:Invitations";

    /// <summary>
    /// The frontend's accept page. <c>{id}</c> and <c>{token}</c> are substituted. The token goes
    /// in the fragment, which browsers do not send to the server and which therefore stays out of
    /// access logs and <c>Referer</c> headers.
    /// </summary>
    public string AcceptUrlTemplate { get; init; } = "http://localhost:3000/invitations/{id}#token={token}";

    /// <summary>
    /// Development only. Writes the link to the log when there is no adapter to deliver it, so a
    /// developer can finish the flow. Off by default and never to be switched on where logs are
    /// shipped anywhere: the link is a credential.
    /// </summary>
    public bool LogUndeliveredLinks { get; init; }
}

/// <summary>
/// Records that an invitation would have been sent, until an email adapter exists.
/// <para>
/// The same kind of placeholder as <c>LoggingNotificationChannel</c>, and loud for the same
/// reason: an invitation that reached nobody must be visible in the log rather than silently
/// successful. It logs at warning and does not throw.
/// </para>
/// <para>
/// What it must not do by default is log the link. A queue-call message names a customer and a
/// template; this message is the means of becoming a member of staff. So the link is written only
/// when <see cref="StaffInvitationOptions.LogUndeliveredLinks"/> is switched on, which the
/// development settings do and nothing else should.
/// </para>
/// </summary>
internal sealed class LoggingStaffInvitationSender(
    IOptions<StaffInvitationOptions> options,
    ILogger<LoggingStaffInvitationSender> logger) : IStaffInvitationSender
{
    public Task SendAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Staff invitation not delivered — no email adapter is registered. "
            + "invitation={InvitationId} email={Email} expires_at={ExpiresAt}",
            message.InvitationId,
            message.Email,
            message.ExpiresAt);

        if (options.Value.LogUndeliveredLinks)
        {
            logger.LogWarning(
                "Development only — accept link for {InvitationId}: {Link}",
                message.InvitationId,
                options.Value.AcceptUrlTemplate
                    .Replace("{id}", Uri.EscapeDataString(message.InvitationId), StringComparison.Ordinal)
                    .Replace("{token}", Uri.EscapeDataString(message.Token), StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }
}
