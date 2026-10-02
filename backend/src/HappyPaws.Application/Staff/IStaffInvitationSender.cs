using System.Security.Cryptography;

namespace HappyPaws.Application.Staff;

/// <summary>What an invitation carries to the person invited.</summary>
/// <param name="InvitationId">Goes in the accept path — <c>/staff/members/invitations/{id}/accept</c>.</param>
/// <param name="Token">The single-use secret. Exists in memory for this one send and is never stored.</param>
/// <param name="Email">Where it goes.</param>
/// <param name="DisplayName">Who it is addressed to.</param>
/// <param name="ExpiresAt">When the link stops working — seven days on.</param>
public sealed record StaffInvitationMessage(
    string InvitationId,
    string Token,
    string Email,
    string DisplayName,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Delivers a staff invitation — "ส่งอีเมล/SMS เชิญ".
/// <para>
/// Its own port rather than a template on <c>INotificationDispatcher</c>, for two reasons. That
/// dispatcher addresses <i>customers</i> and sits behind their consent, and an invited vet is
/// neither a customer nor subject to a marketing opt-out. And this message carries a credential:
/// it must never be written to a notification log the way a queue call is.
/// </para>
/// <para>
/// Called after the transaction commits, like every outbound message here.
/// </para>
/// </summary>
public interface IStaffInvitationSender
{
    Task SendAsync(StaffInvitationMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generates invitation tokens and the hashes stored in their place.
/// <para>
/// Same reasoning as refresh tokens: 256 bits from the system generator, so there is nothing to
/// brute-force and an unsalted SHA-256 is the right stored form. What the database holds cannot be
/// turned back into a link.
/// </para>
/// </summary>
public static class InvitationTokens
{
    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    /// <summary>Compares a presented token against a stored hash without leaking where they differ.</summary>
    public static bool Matches(string presentedToken, string storedHash) =>
        CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(Hash(presentedToken)),
            System.Text.Encoding.ASCII.GetBytes(storedHash));
}
