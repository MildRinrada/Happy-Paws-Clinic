using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// The contract's base URL ends in <c>/v1</c>, and every path in it is relative to that.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ApiVersionTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/v1/services/categories")]
    [InlineData("/services/categories")]
    public async Task An_endpoint_answers_at_the_contracts_address_and_behind_a_proxy_that_stripped_it(string path)
    {
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(path, Token)).StatusCode);
    }

    [Fact]
    public async Task The_refresh_cookie_is_scoped_to_the_auth_endpoints_as_the_caller_addressed_them()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, cancellationToken: Token);
        using var client = fixture.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/v1/auth/staff/login", new { email = staff.Email, password = staff.Password }, Token);

        login.EnsureSuccessStatusCode();

        // A cookie scoped to a bare /auth would never be sent to /v1/auth/refresh.
        Assert.Contains("path=/v1/auth", string.Join(";", login.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/auth/refresh", content: null, Token)).StatusCode);
    }
}
