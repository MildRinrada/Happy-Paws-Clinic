using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>
/// The contract's "HMAC-SHA256 ใน X-Signature", spelled out.
/// <para>
/// Every delivery carries <c>X-Webhook-Timestamp</c> (Unix seconds) and
/// <c>X-Signature: sha256=&lt;hex&gt;</c>, where the hex is HMAC-SHA256, keyed with the
/// subscription's secret, of <c>&lt;timestamp&gt;.&lt;body&gt;</c>. Signing the timestamp with the
/// body lets a receiver refuse a captured delivery replayed later: it checks the signature, then
/// that the timestamp is recent (five minutes is usual). Receivers compare in constant time.
/// </para>
/// </summary>
public static class WebhookSignature
{
    public const string Header = "X-Signature";
    public const string TimestampHeader = "X-Webhook-Timestamp";
    public const string IdHeader = "X-Webhook-Id";
    public const string EventHeader = "X-Webhook-Event";
    public const string Scheme = "sha256=";

    public static string Sign(string secret, long timestamp, string body)
    {
        var payload = Encoding.UTF8.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + "." + body);
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload);

        return Scheme + Convert.ToHexStringLower(mac);
    }

    /// <summary>What a receiver does; here for the tests and for anyone writing a receiver.</summary>
    public static bool Verify(string secret, long timestamp, string body, string signature) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Sign(secret, timestamp, body)),
            Encoding.ASCII.GetBytes(signature));

    /// <summary>A new signing secret: <c>whsec_</c> and 256 random bits.</summary>
    public static string NewSecret() =>
        "whsec_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
