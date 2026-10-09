using System.Net;
using System.Net.Sockets;
using System.Text;
using HappyPaws.Application.Integrations;
using HappyPaws.Infrastructure.Webhooks;

namespace HappyPaws.IntegrationTests.Webhooks;

/// <summary>
/// The real HTTP handler webhooks leave through, against a real socket on this machine: the connect
/// callback refuses an address the rules forbid before any byte is sent, and a redirect comes back
/// as the answer instead of being followed.
/// </summary>
public class WebhookHttpHandlerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Loopback_is_refused_at_connect_time_and_the_listener_never_sees_a_connection()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var policy = new IntegrationPolicy(AllowLocalhost: false, Timeout: TimeSpan.FromSeconds(5));
        using var client = new HttpClient(WebhookHttpHandler.Create(new WebhookAddressGuard(new LoopbackResolver(), policy), policy));

        var failure = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.PostAsync($"http://hooks.example.test:{port}/x", new StringContent("{}"), Token));

        Assert.IsType<WebhookAddressBlockedException>(failure.InnerException);
        Assert.False(listener.Pending());
    }

    [Fact]
    public async Task A_redirect_is_returned_not_followed()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var connections = 0;

        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptSocketAsync(Token);
            Interlocked.Increment(ref connections);

            var buffer = new byte[8_192];
            await socket.ReceiveAsync(buffer, SocketFlags.None, Token);

            var answer = "HTTP/1.1 302 Found\r\nLocation: http://169.254.169.254/latest/meta-data\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
            await socket.SendAsync(Encoding.ASCII.GetBytes(answer), SocketFlags.None, Token);
        }, Token);

        // Development's rule, so the test can reach its own listener; private ranges stay refused.
        var policy = new IntegrationPolicy(AllowLocalhost: true, Timeout: TimeSpan.FromSeconds(5));
        var handler = WebhookHttpHandler.Create(new WebhookAddressGuard(new LoopbackResolver(), policy), policy);

        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseProxy);

        using var client = new HttpClient(handler);
        using var response = await client.PostAsync($"http://hooks.example.test:{port}/x", new StringContent("{}"), Token);

        await server;

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(1, connections);
    }

    /// <summary>Every name is this machine.</summary>
    private sealed class LoopbackResolver : IWebhookAddressResolver
    {
        public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken = default) =>
            Task.FromResult(new[] { IPAddress.Loopback });
    }
}
