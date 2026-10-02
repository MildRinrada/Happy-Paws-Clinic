using System.Net.Http.Headers;
using HappyPaws.Application.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Mints access tokens through the production <see cref="IAccessTokenIssuer"/>.
/// <para>
/// Deliberately not a hand-rolled JWT builder. Using the real issuer is what makes these tests
/// prove the round-trip: if the issuer and the validator ever disagree about a claim name, the
/// tests fail instead of quietly passing against a token no production code would ever produce.
/// </para>
/// </summary>
public static class TestTokens
{
    public static string Staff(
        ApiFixture fixture,
        string userId = "usr_test",
        string staffId = "stf_test",
        IReadOnlyCollection<string>? scopes = null,
        IReadOnlyCollection<string>? branches = null,
        bool twoFactorSatisfied = true)
    {
        using var scope = fixture.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

        return issuer.Issue(new AccessTokenRequest(
            UserId: userId,
            SessionId: "ses_test",
            StaffId: staffId,
            CustomerId: null,
            BranchIds: branches ?? [],
            Scopes: scopes ?? [],
            TwoFactorSatisfied: twoFactorSatisfied)).Token;
    }

    public static string Customer(
        ApiFixture fixture,
        string userId = "usr_owner",
        string customerId = "cus_test")
    {
        using var scope = fixture.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

        return issuer.Issue(new AccessTokenRequest(
            UserId: userId,
            SessionId: "ses_test",
            StaffId: null,
            CustomerId: customerId,
            BranchIds: [],
            Scopes: [Scopes_Portal],
            TwoFactorSatisfied: false)).Token;
    }

    private const string Scopes_Portal = "portal";

    public static HttpClient Authenticated(this ApiFixture fixture, string token)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
