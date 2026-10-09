using System.Net;
using System.Net.Sockets;
using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>Looks a host name up. An interface so tests can say what a name resolves to.</summary>
public interface IWebhookAddressResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken = default);
}

/// <summary>The system resolver.</summary>
internal sealed class DnsWebhookAddressResolver : IWebhookAddressResolver
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken = default) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);
}

/// <summary>A webhook address the rules refuse. Turned into the delivery error <c>address_blocked</c>.</summary>
public sealed class WebhookAddressBlockedException(string message) : Exception(message);

/// <summary>
/// The SSRF check at send time. Resolves the host and refuses it when <b>any</b> address it
/// resolves to is not public (<see cref="WebhookAddressRules"/>). Any, not all: a name that
/// answers with one public and one private address must not get a chance to be connected through
/// the private one.
/// <para>
/// Called twice per attempt: by the sender before the request is built, and by the HTTP handler's
/// connect callback, which then connects to exactly the address it checked. The second call is
/// what defeats DNS rebinding (a name that resolves to a public address for the first lookup and a
/// private one for the second).
/// </para>
/// </summary>
public sealed class WebhookAddressGuard(IWebhookAddressResolver resolver, IntegrationPolicy policy)
{
    public async Task<IPAddress[]> CheckAsync(string host, CancellationToken cancellationToken = default)
    {
        var name = host.Trim('[', ']');

        if (IPAddress.TryParse(name, out var literal))
        {
            return Allowed([literal], name);
        }

        if (WebhookAddressRules.IsLocalName(name) && !(policy.AllowLocalhost && name.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            throw new WebhookAddressBlockedException($"{name} is a local name");
        }

        IPAddress[] addresses;

        try
        {
            addresses = await resolver.ResolveAsync(name, cancellationToken);
        }
        catch (SocketException exception)
        {
            throw new HttpRequestException($"cannot resolve {name}", exception);
        }

        return Allowed(addresses, name);
    }

    private IPAddress[] Allowed(IPAddress[] addresses, string name)
    {
        if (addresses.Length == 0)
        {
            throw new HttpRequestException($"{name} has no address");
        }

        if (addresses.FirstOrDefault(address => !WebhookAddressRules.IsAllowed(address, policy.AllowLocalhost)) is { } blocked)
        {
            throw new WebhookAddressBlockedException($"{name} resolves to {blocked}, which is not a public address");
        }

        return addresses;
    }
}
