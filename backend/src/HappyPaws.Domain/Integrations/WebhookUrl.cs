using System.Net;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The shape a webhook URL must have before it is stored.
/// <para>
/// <c>https</c> only: the payload is signed but not encrypted, and it names appointments, bills
/// and pets. Plain <c>http</c> is accepted for <c>localhost</c> alone, and only when the caller
/// says this is Development, so a developer can point a webhook at a tool on their own machine.
/// No user name or password in the URL (it would sit in the database and in logs), no fragment,
/// and a host that is not an obviously private address. Whether the name resolves to a private
/// address is decided at send time, by <see cref="WebhookAddressRules"/>.
/// </para>
/// </summary>
public static class WebhookUrl
{
    public const int MaxLength = 2_000;

    /// <summary>Null when <paramref name="value"/> is acceptable; otherwise why not, in Thai.</summary>
    public static string? Problem(string? value, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "ต้องระบุ URL";
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return $"URL ยาวได้ไม่เกิน {MaxLength} ตัวอักษร";
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return "URL ไม่ถูกต้อง";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return "URL ต้องไม่มีชื่อผู้ใช้หรือรหัสผ่าน";
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            return "URL ต้องไม่มีส่วน #";
        }

        var local = IsLoopbackHost(uri);

        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            return isDevelopment && local ? null : "URL ต้องขึ้นต้นด้วย https://";
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return "URL ต้องขึ้นต้นด้วย https://";
        }

        if (local)
        {
            return isDevelopment ? null : "ส่งไปที่เครื่องนี้หรือเครือข่ายภายในไม่ได้";
        }

        if (WebhookAddressRules.IsLocalName(uri.IdnHost))
        {
            return "ส่งไปที่เครื่องนี้หรือเครือข่ายภายในไม่ได้";
        }

        if (IPAddress.TryParse(uri.IdnHost.Trim('[', ']'), out var literal) && !WebhookAddressRules.IsAllowed(literal))
        {
            return "ส่งไปที่เครื่องนี้หรือเครือข่ายภายในไม่ได้";
        }

        return null;
    }

    private static bool IsLoopbackHost(Uri uri) =>
        uri.IsLoopback || uri.IdnHost.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
