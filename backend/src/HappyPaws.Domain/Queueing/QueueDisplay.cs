using System.Security.Cryptography;
using System.Text;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Domain.Queueing;

/// <summary>
/// One waiting-room screen, bound to one branch (<c>docs/contract-extensions.md § 30</c>). Not in
/// the API reference.
/// <para>
/// <b>Why it exists.</b> Before this, the TV in the waiting room had to be signed in with a staff
/// account, so anyone who reached its keyboard reached that account's console. A display holds a
/// credential of its own that opens exactly two routes, the display board and that branch's queue
/// stream, and nothing else.
/// </para>
/// <para>
/// <b>Two secrets, each shown once.</b> Creating a display returns a short setup code
/// for a person to type on the TV (or follow as a link), valid for <see cref="SetupLifetime"/> and
/// for one use. The TV trades it for the long-lived token (<c>hpdsp_</c> and 256 random bits),
/// which only the TV's own server ever sees. The database keeps the SHA-256 of each and never the
/// value, the same rule as API keys and calendar links. The token itself never appears in a URL,
/// on a screen or in a browser's history.
/// </para>
/// <para>
/// <b>It ends.</b> <see cref="RevokedAt"/> stops it on the next request, because authentication
/// reads this row every time. <see cref="ExpiresAt"/> is optional: a screen on a wall is meant to
/// run until someone takes it down.
/// </para>
/// </summary>
public sealed class QueueDisplay : Entity
{
    public const string TokenPrefix = "hpdsp_";

    public const int NameMaxLength = 80;

    /// <summary>Eight characters, shown as <c>ABCD-EFGH</c>.</summary>
    public const int SetupCodeLength = 8;

    /// <summary>How long the code may wait between the console and the TV.</summary>
    public static readonly TimeSpan SetupLifetime = TimeSpan.FromMinutes(30);

    /// <summary>The longest a display may be issued for when it is given an end at all.</summary>
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(730);

    /// <summary>How stale <see cref="LastSeenAt"/> may get before a request writes it again.</summary>
    public static readonly TimeSpan LastSeenResolution = TimeSpan.FromMinutes(1);

    /// <summary>No 0, 1, I, L, O or U: a code read off one screen and typed on another.</summary>
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTVWXYZ23456789";

    private QueueDisplay()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the token, lowercase hex. Null until the TV has claimed its code.</summary>
    public string? TokenHash { get; private set; }

    /// <summary>SHA-256 of the setup code. Cleared the moment it is used.</summary>
    public string? SetupCodeHash { get; private set; }

    public DateTimeOffset? SetupExpiresAt { get; private set; }

    /// <summary>When the TV traded its code for the token.</summary>
    public DateTimeOffset? ClaimedAt { get; private set; }

    /// <summary>Null: until revoked.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string CreatedByStaffId { get; private set; } = string.Empty;

    /// <summary>A new display, and its setup code to show once. The code is not kept anywhere.</summary>
    public static (QueueDisplay Display, string SetupCode) Create(
        string name,
        string branchId,
        DateTimeOffset? expiresAt,
        DateTimeOffset now,
        string createdByStaffId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new DomainException("invalid_display", "ชื่อจอต้องมีและยาวไม่เกิน 80 ตัวอักษร");
        }

        if (expiresAt is { } end && (end <= now || end > now + MaxLifetime + TimeSpan.FromMinutes(1)))
        {
            throw new DomainException("invalid_display", "วันหมดอายุต้องอยู่ในอนาคตและไม่เกิน 2 ปี");
        }

        var code = NewSetupCode();

        var display = new QueueDisplay
        {
            Id = EntityId.New(EntityId.QueueDisplay),
            Name = name.Trim(),
            BranchId = branchId,
            SetupCodeHash = Hash(code),
            SetupExpiresAt = now + SetupLifetime,
            ExpiresAt = expiresAt,
            CreatedByStaffId = createdByStaffId,
        };

        return (display, FormatCode(code));
    }

    /// <summary>A fresh token for the TV. The caller stores its hash; the value goes to the TV only.</summary>
    public static string NewToken() =>
        TokenPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string value) => ApiKey.Hash(value);

    /// <summary>
    /// A typed code as it is hashed: capitals, without spaces or dashes, so <c>abcd efgh</c> and
    /// <c>ABCD-EFGH</c> are the same code. Null when what is left cannot be one of ours.
    /// </summary>
    public static string? NormaliseCode(string? typed)
    {
        if (typed is null)
        {
            return null;
        }

        var code = new string([.. typed.Where(c => c is not (' ' or '-')).Select(char.ToUpperInvariant)]);

        return code.Length == SetupCodeLength && code.All(CodeAlphabet.Contains) ? code : null;
    }

    /// <summary>Whether a presented token is shaped like one, before anything is looked up.</summary>
    public static bool LooksLikeToken(string presented) =>
        presented.StartsWith(TokenPrefix, StringComparison.Ordinal) && presented.Length == TokenPrefix.Length + 43;

    /// <summary>The token works now: claimed, not revoked, not past its end.</summary>
    public bool IsUsableAt(DateTimeOffset now) =>
        TokenHash is not null && RevokedAt is null && (ExpiresAt is not { } end || now < end);

    /// <summary>The setup code can still be traded for a token.</summary>
    public bool CanBeClaimedAt(DateTimeOffset now) =>
        TokenHash is null && SetupCodeHash is not null && RevokedAt is null
        && SetupExpiresAt is { } until && now < until
        && (ExpiresAt is not { } end || now < end);

    /// <summary>Compares in constant time.</summary>
    public bool TokenMatches(string presented) =>
        TokenHash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(TokenHash), Encoding.ASCII.GetBytes(Hash(presented)));

    /// <summary>
    /// <c>revoked</c>, <c>expired</c>, <c>active</c>, <c>pending_setup</c> or <c>setup_expired</c>.
    /// Revoked wins, because that was a decision.
    /// </summary>
    public string StateAt(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return "revoked";
        }

        if (ExpiresAt is { } end && now >= end)
        {
            return "expired";
        }

        if (TokenHash is not null)
        {
            return "active";
        }

        return SetupExpiresAt is { } until && now < until ? "pending_setup" : "setup_expired";
    }

    public bool NeedsLastSeenUpdate(DateTimeOffset now) =>
        LastSeenAt is not { } last || now - last >= LastSeenResolution;

    private static string NewSetupCode() =>
        string.Create(SetupCodeLength, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }
        });

    private static string FormatCode(string code) => $"{code[..4]}-{code[4..]}";
}
