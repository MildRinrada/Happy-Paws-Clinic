using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// The cross-cutting foundation stands up: the service graph resolves, migrations apply, the
/// pipeline is ordered correctly, and unauthenticated traffic is refused in the contract's
/// envelope rather than with an empty body.
/// </summary>
[Collection(nameof(ApiCollection))]
public class FoundationTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void Service_graph_resolves()
    {
        // ValidateOnBuild is enabled in the fixture, so a missing or mis-scoped registration
        // fails here instead of on the first production request that touches it.
        using var scope = fixture.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<ICurrentUser>());
        Assert.NotNull(provider.GetRequiredService<IRequestContext>());
        Assert.NotNull(provider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(provider.GetRequiredService<IIdempotencyStore>());
        Assert.NotNull(provider.GetRequiredService<IAccessTokenIssuer>());
        Assert.NotNull(provider.GetRequiredService<IClock>());
        Assert.NotNull(provider.GetRequiredService<RequestValidator>());
        Assert.NotNull(provider.GetRequiredService<BranchGuard>());
        Assert.NotNull(provider.GetRequiredService<PetAccessGuard>());
        Assert.NotNull(provider.GetRequiredService<HappyPawsDbContext>());
    }

    [Fact]
    public async Task Migrations_apply_cleanly_to_a_real_postgres()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Token));
    }

    [Fact]
    public void Clock_uses_the_clinic_time_zone()
    {
        using var scope = fixture.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        // Thailand is UTC+7 year-round; a wrong zone would offer the wrong booking slots all day.
        Assert.Equal(TimeSpan.FromHours(7), clock.NowInClinicTime().Offset);
    }

    [Fact]
    public async Task Unauthenticated_request_is_refused_in_the_shared_error_envelope()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/_probe/identity", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal(ErrorCodes.Unauthenticated, error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("request_id").GetString()));
    }

    [Fact]
    public async Task A_garbage_bearer_token_is_refused_the_same_way()
    {
        using var client = fixture.Authenticated("not-a-real-token");

        var response = await client.GetAsync("/_probe/identity", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(ErrorCodes.Unauthenticated, body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Request_id_is_returned_so_a_caller_can_quote_it_in_support()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/_probe/identity", Token);

        Assert.True(response.Headers.TryGetValues("X-Request-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values!.Single()));
    }

    [Fact]
    public async Task A_caller_supplied_request_id_is_echoed_back_for_end_to_end_tracing()
    {
        using var client = fixture.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/_probe/public");
        request.Headers.Add("X-Request-Id", "req-from-frontend-123");

        var response = await client.SendAsync(request, Token);

        Assert.Equal("req-from-frontend-123", response.Headers.GetValues("X-Request-Id").Single());
    }

    [Fact]
    public async Task A_hostile_request_id_is_stripped_before_it_reaches_headers_or_logs()
    {
        // The value is echoed into a response header and into every log line for the request, so
        // anything outside [A-Za-z0-9-_] must not survive — that is what closes off header
        // splitting and log forging.
        using var client = fixture.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/_probe/public");
        request.Headers.TryAddWithoutValidation("X-Request-Id", "abc\"; injected=1 <script>");

        var response = await client.SendAsync(request, Token);

        Assert.Equal("abcinjected1script", response.Headers.GetValues("X-Request-Id").Single());
    }

    [Fact]
    public async Task An_overlong_request_id_is_truncated()
    {
        using var client = fixture.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/_probe/public");
        request.Headers.Add("X-Request-Id", new string('a', 500));

        var response = await client.SendAsync(request, Token);

        Assert.Equal(64, response.Headers.GetValues("X-Request-Id").Single().Length);
    }
}
