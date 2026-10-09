using System.Net;
using System.Net.Sockets;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// Which network addresses an outbound webhook may reach. The SSRF rule, in one place.
/// <para>
/// A webhook URL is typed by a person and then fetched by the server, from inside the clinic's
/// network. Without this rule, <c>https://10.0.0.5/admin</c> or the cloud metadata address
/// <c>169.254.169.254</c> would be reachable by anyone who can create a webhook. So the server
/// only talks to public addresses: loopback, private, link-local, carrier-grade NAT, multicast and
/// reserved ranges are refused, for IPv4 and IPv6, including an IPv4 address dressed up as IPv6.
/// </para>
/// <para>
/// The rule is applied twice. When the subscription is created, to a URL whose host is an IP
/// literal or an obviously local name. And at send time, to every address the host name resolves
/// to, immediately before connecting, so a name that pointed somewhere public yesterday and to
/// <c>127.0.0.1</c> today (DNS rebinding) is still refused.
/// </para>
/// </summary>
public static class WebhookAddressRules
{
    /// <summary>
    /// True when the server may connect to <paramref name="address"/>. Loopback is allowed only
    /// when <paramref name="allowLoopback"/> is set, which only Development does.
    /// </summary>
    public static bool IsAllowed(IPAddress address, bool allowLoopback = false)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return allowLoopback;
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicV4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicV6(address),
            _ => false,
        };
    }

    /// <summary>
    /// Host names that can only mean this machine or its private network, refused before any
    /// lookup: <c>localhost</c> and anything under <c>.localhost</c>, <c>.local</c> or
    /// <c>.internal</c> (which is how cloud providers name their metadata service).
    /// </summary>
    public static bool IsLocalName(string host)
    {
        var name = host.Trim().TrimEnd('.').ToLowerInvariant();

        return name is "localhost" or "metadata" or "metadata.google.internal"
            || name.EndsWith(".localhost", StringComparison.Ordinal)
            || name.EndsWith(".local", StringComparison.Ordinal)
            || name.EndsWith(".internal", StringComparison.Ordinal);
    }

    private static bool IsPublicV4(byte[] b) =>
        !(b[0] == 0                                        // 0.0.0.0/8 "this network"
          || b[0] == 10                                    // 10.0.0.0/8 private
          || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)    // 100.64.0.0/10 carrier-grade NAT
          || b[0] == 127                                   // 127.0.0.0/8 loopback
          || (b[0] == 169 && b[1] == 254)                  // 169.254.0.0/16 link-local, cloud metadata
          || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)     // 172.16.0.0/12 private
          || (b[0] == 192 && b[1] == 0 && b[2] == 0)       // 192.0.0.0/24 protocol assignments
          || (b[0] == 192 && b[1] == 168)                  // 192.168.0.0/16 private
          || (b[0] == 198 && (b[1] == 18 || b[1] == 19))   // 198.18.0.0/15 benchmarking
          || b[0] >= 224);                                 // multicast, reserved, broadcast

    private static bool IsPublicV6(IPAddress address)
    {
        if (address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any)
            || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        var b = address.GetAddressBytes();

        // fc00::/7 unique local (includes fd00:ec2::254, the AWS metadata address over IPv6).
        if ((b[0] & 0xFE) == 0xFC)
        {
            return false;
        }

        // 64:ff9b::/96 NAT64 and ::/96 IPv4-compatible: an IPv4 address inside, judged as IPv4.
        var embedsV4 = (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B && b.Skip(4).Take(8).All(x => x == 0))
            || b.Take(12).All(x => x == 0);

        return !embedsV4 || IsPublicV4(b[12..]);
    }
}
