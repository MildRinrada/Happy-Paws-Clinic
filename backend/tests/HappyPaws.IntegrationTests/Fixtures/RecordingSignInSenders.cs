using System.Collections.Concurrent;
using HappyPaws.Application.Identity;
using HappyPaws.Domain.Identity;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for SMS and email so a test can read the one-time code or the reset link it was sent,
/// the way a customer reads their phone. Only the outermost adapter is replaced: everything that
/// decides whether to send is real.
/// </summary>
public sealed class RecordingSignInSenders : IOtpSender, IPasswordResetSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new();
    private readonly ConcurrentDictionary<string, string> _links = new();
    private readonly ConcurrentDictionary<string, int> _codeCounts = new();

    public Task SendAsync(string phoneDigits, string code, OtpPurpose purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        _codes[phoneDigits] = code;
        _codeCounts.AddOrUpdate(phoneDigits, 1, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task SendAsync(PasswordResetMessage message, CancellationToken cancellationToken = default)
    {
        _links[message.UserId] = message.Token;
        return Task.CompletedTask;
    }

    /// <summary>The last code texted to a number, or null if none was.</summary>
    public string? CodeFor(string phoneDigits) => _codes.TryGetValue(phoneDigits, out var code) ? code : null;

    public int CodesSentTo(string phoneDigits) => _codeCounts.TryGetValue(phoneDigits, out var count) ? count : 0;

    /// <summary>The token in the last reset link sent for an account, or null if none was.</summary>
    public string? ResetTokenFor(string userId) => _links.TryGetValue(userId, out var token) ? token : null;
}
