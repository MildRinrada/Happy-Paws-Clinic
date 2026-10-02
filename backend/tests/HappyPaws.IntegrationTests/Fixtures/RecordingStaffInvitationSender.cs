using System.Collections.Concurrent;
using HappyPaws.Application.Staff;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for the email adapter so a test can follow the link an invitee would have received.
/// <para>
/// The token exists nowhere else a test could reach: the response to the inviter does not carry
/// it and the database holds only its hash. That is the property under test, and it is also why
/// the only way to accept an invitation here is to read what was "sent".
/// </para>
/// </summary>
public sealed class RecordingStaffInvitationSender : IStaffInvitationSender
{
    private readonly ConcurrentQueue<StaffInvitationMessage> _sent = new();

    public Task SendAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Every message sent to one address, oldest first. The suite shares a host, so never the whole queue.</summary>
    public IReadOnlyList<StaffInvitationMessage> SentTo(string email) =>
        [.. _sent.Where(message => string.Equals(message.Email, email, StringComparison.OrdinalIgnoreCase))];

    /// <summary>The link most recently sent to an address.</summary>
    public StaffInvitationMessage LatestTo(string email) => SentTo(email)[^1];
}
