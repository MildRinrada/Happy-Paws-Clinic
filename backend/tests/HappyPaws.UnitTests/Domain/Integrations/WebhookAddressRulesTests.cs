using System.Net;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>
/// The SSRF rule: which addresses a webhook may reach. Every private, local and reserved range is
/// listed by an address inside it, including the cloud metadata address and IPv4 hidden in IPv6.
/// </summary>
public class WebhookAddressRulesTests
{
    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("127.0.0.1")]
    [InlineData("127.10.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("192.0.0.170")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd00:ec2::254")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("64:ff9b::a00:1")]
    public void Private_local_and_reserved_addresses_are_refused(string address)
    {
        Assert.False(WebhookAddressRules.IsAllowed(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("93.184.216.34")]
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]
    [InlineData("100.128.0.1")]
    [InlineData("2606:4700::6810:84e5")]
    [InlineData("::ffff:93.184.216.34")]
    public void Public_addresses_are_allowed(string address)
    {
        Assert.True(WebhookAddressRules.IsAllowed(IPAddress.Parse(address)));
    }

    [Fact]
    public void Loopback_is_allowed_only_when_asked_for_and_private_ranges_never()
    {
        Assert.True(WebhookAddressRules.IsAllowed(IPAddress.Loopback, allowLoopback: true));
        Assert.True(WebhookAddressRules.IsAllowed(IPAddress.IPv6Loopback, allowLoopback: true));
        Assert.False(WebhookAddressRules.IsAllowed(IPAddress.Parse("10.0.0.1"), allowLoopback: true));
        Assert.False(WebhookAddressRules.IsAllowed(IPAddress.Parse("169.254.169.254"), allowLoopback: true));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST.")]
    [InlineData("api.localhost")]
    [InlineData("printer.local")]
    [InlineData("metadata.google.internal")]
    [InlineData("db.internal")]
    public void Local_names_are_recognised(string host)
    {
        Assert.True(WebhookAddressRules.IsLocalName(host));
    }

    [Fact]
    public void A_public_name_is_not_a_local_name()
    {
        Assert.False(WebhookAddressRules.IsLocalName("hooks.example.com"));
    }
}

/// <summary>What a webhook URL may look like when it is stored.</summary>
public class WebhookUrlTests
{
    [Theory]
    [InlineData("https://hooks.example.com/clinic")]
    [InlineData("https://hooks.example.com:8443/clinic?token=abc")]
    [InlineData("https://93.184.216.34/hook")]
    public void Https_to_a_public_host_is_accepted(string url)
    {
        Assert.Null(WebhookUrl.Problem(url, isDevelopment: false));
    }

    [Theory]
    [InlineData("http://hooks.example.com/clinic")]
    [InlineData("ftp://hooks.example.com/clinic")]
    [InlineData("https://user:pass@hooks.example.com/")]
    [InlineData("https://hooks.example.com/#part")]
    [InlineData("https://localhost/hook")]
    [InlineData("http://localhost:5000/hook")]
    [InlineData("https://10.0.0.5/hook")]
    [InlineData("https://[::1]/hook")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://metadata.google.internal/")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Anything_else_is_refused_in_production(string url)
    {
        Assert.NotNull(WebhookUrl.Problem(url, isDevelopment: false));
    }

    [Fact]
    public void Development_allows_http_to_localhost_and_nothing_private()
    {
        Assert.Null(WebhookUrl.Problem("http://localhost:5000/hook", isDevelopment: true));
        Assert.Null(WebhookUrl.Problem("http://127.0.0.1:5000/hook", isDevelopment: true));
        Assert.NotNull(WebhookUrl.Problem("http://hooks.example.com/hook", isDevelopment: true));
        Assert.NotNull(WebhookUrl.Problem("https://10.0.0.5/hook", isDevelopment: true));
    }

    [Fact]
    public void An_overlong_url_is_refused()
    {
        Assert.NotNull(WebhookUrl.Problem("https://hooks.example.com/" + new string('a', WebhookUrl.MaxLength), isDevelopment: false));
    }
}
