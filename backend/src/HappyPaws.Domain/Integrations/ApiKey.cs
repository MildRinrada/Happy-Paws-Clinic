using System.Security.Cryptography;
using System.Text;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// A key a machine client uses instead of a staff login — <c>POST /staff/api-keys</c>.
/// <para>
/// <b>Shape.</b> <c>hp_&lt;prefix&gt;_&lt;secret&gt;</c>. The prefix is ten random characters
/// that identify the key in lists and logs without revealing it; the secret is 256 random bits.
/// The whole key is shown once, in the answer to the create call. The database keeps
/// <see cref="Prefix"/> and <see cref="SecretHash"/>, the SHA-256 of the whole key, and nothing
/// else. SHA-256 without a work factor is right for the same reason it is for refresh tokens: there
/// are 256 bits to guess, so there is nothing for a slow hash to slow down.
/// </para>
/// <para>
/// <b>What it may do.</b> Exactly <see cref="Scopes"/>, which were a subset of its creator's scopes
/// when it was made, and never more than the creator holds <i>now</i>: authentication intersects
/// the two on every request, so taking a scope away from a person takes it away from their keys.
/// The same for branches. A key whose creator is deactivated stops working.
/// </para>
/// <para>
/// <b>It ends.</b> Every key has an <see cref="ExpiresAt"/>, at most
/// <see cref="MaxLifetime"/> after it was made. The contract has no endpoint that revokes a key, so
/// expiry is the only way one is guaranteed to stop; <see cref="RevokedAt"/> exists for the
/// revocation endpoint proposed in <c>docs/features/integrations.md</c>.
/// </para>
/// </summary>
public sealed class ApiKey : Entity
{
    public const string KeyPrefix = "hp_";

    public const int NameMaxLength = 100;

    public const int PrefixLength = 10;

    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(90);

    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(365);

    /// <summary>How stale <see cref="LastUsedAt"/> may get before a request writes it again.</summary>
    public static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(1);

    private const string PrefixAlphabet = "abcdefghijkmnpqrstuvwxyz23456789";

    private readonly List<string> _scopes = [];
    private readonly List<string> _branchIds = [];

    private ApiKey()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The public part, e.g. <c>k7m2qx9ab4</c>. Unique; shown in lists.</summary>
    public string Prefix { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the whole key, lowercase hex. The key itself is never stored.</summary>
    public string SecretHash { get; private set; } = string.Empty;

    public IReadOnlyList<string> Scopes => _scopes;

    /// <summary>Empty means every branch its creator can reach.</summary>
    public IReadOnlyList<string> BranchIds => _branchIds;

    public DateTimeOffset ExpiresAt { get; private set; }

    public string CreatedByUserId { get; private set; } = string.Empty;

    public string CreatedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset? LastUsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>A new key, and the plaintext to show once. The plaintext is not kept anywhere.</summary>
    public static (ApiKey Key, string Plaintext) Issue(
        string name,
        IEnumerable<string> scopes,
        IEnumerable<string> branchIds,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        string createdByUserId,
        string createdByStaffId)
    {
        if (expiresAt <= now || expiresAt > now + MaxLifetime + TimeSpan.FromMinutes(1))
        {
            throw new DomainException("invalid_api_key", "วันหมดอายุต้องอยู่ในอนาคตและไม่เกิน 365 วัน");
        }

        var prefix = RandomPrefix();
        var plaintext = $"{KeyPrefix}{prefix}_{Base64Url(RandomNumberGenerator.GetBytes(32))}";

        var key = new ApiKey
        {
            Id = EntityId.New(EntityId.ApiKey),
            Name = name.Trim(),
            Prefix = prefix,
            SecretHash = Hash(plaintext),
            ExpiresAt = expiresAt,
            CreatedByUserId = createdByUserId,
            CreatedByStaffId = createdByStaffId,
        };

        key._scopes.AddRange(scopes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        key._branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

        return (key, plaintext);
    }

    public static string Hash(string plaintext) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));

    /// <summary>
    /// The prefix part of a presented key, or null when it is not shaped like one of ours.
    /// Cheap, so a malformed bearer token is turned away without touching the database.
    /// </summary>
    public static string? PrefixOf(string presented)
    {
        if (!presented.StartsWith(KeyPrefix, StringComparison.Ordinal)
            || presented.Length != KeyPrefix.Length + PrefixLength + 1 + 43
            || presented[KeyPrefix.Length + PrefixLength] != '_')
        {
            return null;
        }

        var prefix = presented.Substring(KeyPrefix.Length, PrefixLength);

        return prefix.All(PrefixAlphabet.Contains) ? prefix : null;
    }

    /// <summary>Compares in constant time, so the comparison itself leaks nothing about the hash.</summary>
    public bool Matches(string presented) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(SecretHash),
            Encoding.ASCII.GetBytes(Hash(presented)));

    public bool IsUsableAt(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public bool NeedsLastUsedUpdate(DateTimeOffset now) =>
        LastUsedAt is not { } last || now - last >= LastUsedResolution;

    private static string RandomPrefix() =>
        string.Create(PrefixLength, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = PrefixAlphabet[RandomNumberGenerator.GetInt32(PrefixAlphabet.Length)];
            }
        });

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
