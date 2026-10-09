using System.Net;
using System.Net.Sockets;
using HappyPaws.Application.Integrations;

namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>
/// The HTTP handler webhooks go out through. Every setting here is a security decision:
/// <list type="bullet">
///   <item><description><b>No redirects.</b> A receiver answering <c>302 Location: http://169.254.169.254/</c> would otherwise walk the server into its own network. A 3xx is recorded as a failure.</description></item>
///   <item><description><b>No proxy.</b> A proxy would make the connection on our behalf, to an address this process never checks.</description></item>
///   <item><description><b>Checked connect.</b> The connect callback resolves the host, refuses it if any address is not public (<see cref="WebhookAddressGuard"/>), and connects to an address it just checked, so DNS cannot be changed between check and use.</description></item>
///   <item><description><b>No cookies, short-lived connections, a connect timeout.</b></description></item>
/// </list>
/// </summary>
public static class WebhookHttpHandler
{
    public static SocketsHttpHandler Create(WebhookAddressGuard guard, IntegrationPolicy policy) =>
        new()
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = policy.Timeout,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxResponseHeadersLength = 32,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await guard.CheckAsync(context.DnsEndPoint.Host, cancellationToken);

                Exception? last = null;

                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception exception) when (exception is SocketException or OperationCanceledException)
                    {
                        socket.Dispose();
                        last = exception;

                        if (exception is OperationCanceledException)
                        {
                            throw;
                        }
                    }
                }

                throw new HttpRequestException($"cannot connect to {context.DnsEndPoint.Host}", last);
            },
        };
}
