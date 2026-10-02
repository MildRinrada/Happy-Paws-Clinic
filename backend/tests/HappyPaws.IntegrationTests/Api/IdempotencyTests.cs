using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// Idempotency against the real database.
/// <para>
/// The probe endpoint returns a fresh value on every execution, so a replayed response is
/// detectable: if the token differs between two calls with the same key, the operation ran twice —
/// which on a real endpoint means a double booking or a double charge.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class IdempotencyTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Client() =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Wildcard]));

    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<string> TokenValueAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("token").GetString()!;

    [Fact]
    public async Task A_marked_endpoint_refuses_a_request_with_no_key()
    {
        using var client = Client();

        var response = await client.PostAsync("/_probe/idempotent", Body("{}"), Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(ErrorCodes.IdempotencyKeyRequired, error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task The_same_key_and_body_replays_the_first_response_instead_of_running_again()
    {
        using var client = Client();
        var key = Guid.NewGuid().ToString();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{\"a\":1}") };
        first.Headers.Add("Idempotency-Key", key);
        var firstResponse = await client.SendAsync(first, Token);

        using var second = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{\"a\":1}") };
        second.Headers.Add("Idempotency-Key", key);
        var secondResponse = await client.SendAsync(second, Token);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        // Same value means the handler did not execute a second time.
        Assert.Equal(await TokenValueAsync(firstResponse), await TokenValueAsync(secondResponse));
        Assert.True(secondResponse.Headers.Contains("Idempotent-Replay"));
        Assert.False(firstResponse.Headers.Contains("Idempotent-Replay"));
    }

    [Fact]
    public async Task A_different_key_runs_the_operation_again()
    {
        using var client = Client();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
        first.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using var second = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
        second.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var firstToken = await TokenValueAsync(await client.SendAsync(first, Token));
        var secondToken = await TokenValueAsync(await client.SendAsync(second, Token));

        Assert.NotEqual(firstToken, secondToken);
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_body_is_refused_rather_than_replayed()
    {
        // Replaying here would hide a client bug and could confirm an operation the caller never
        // actually requested.
        using var client = Client();
        var key = Guid.NewGuid().ToString();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{\"a\":1}") };
        first.Headers.Add("Idempotency-Key", key);
        await client.SendAsync(first, Token);

        using var second = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{\"a\":2}") };
        second.Headers.Add("Idempotency-Key", key);
        var response = await client.SendAsync(second, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(ErrorCodes.IdempotencyKeyReused, error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_key_is_scoped_per_account_so_two_callers_cannot_collide()
    {
        var key = Guid.NewGuid().ToString();

        using var alice = fixture.Authenticated(
            TestTokens.Staff(fixture, userId: "usr_alice", scopes: [Scopes.Wildcard]));
        using var bob = fixture.Authenticated(
            TestTokens.Staff(fixture, userId: "usr_bob", scopes: [Scopes.Wildcard]));

        using var aliceRequest = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
        aliceRequest.Headers.Add("Idempotency-Key", key);

        using var bobRequest = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
        bobRequest.Headers.Add("Idempotency-Key", key);

        var aliceToken = await TokenValueAsync(await alice.SendAsync(aliceRequest, Token));
        var bobToken = await TokenValueAsync(await bob.SendAsync(bobRequest, Token));

        // Bob must get his own execution, not a replay of Alice's response.
        Assert.NotEqual(aliceToken, bobToken);
    }

    [Fact]
    public async Task A_faulted_request_releases_the_claim_so_a_retry_is_still_possible()
    {
        // A 5xx is not a settled outcome — the operation may not have happened. Holding the claim
        // would refuse the client's legitimate retry for the next 24 hours.
        using var client = Client();
        var key = Guid.NewGuid().ToString();

        using var failing = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent-failure") { Content = Body("{}") };
        failing.Headers.Add("Idempotency-Key", key);
        var failed = await client.SendAsync(failing, Token);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);

        // Same key on the working endpoint: the claim from the faulted attempt must be gone.
        using var retry = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
        retry.Headers.Add("Idempotency-Key", key);
        var retried = await client.SendAsync(retry, Token);

        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        Assert.False(retried.Headers.Contains("Idempotent-Replay"));
    }

    [Fact]
    public async Task Concurrent_retries_of_the_same_key_execute_the_operation_only_once()
    {
        // The real race: a double-tapped Confirm button firing two requests at once. Exactly one
        // must execute; the other either replays or is told to back off.
        using var client = Client();
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/_probe/idempotent") { Content = Body("{}") };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request, Token);
        }));

        var executed = responses.Count(r => r.IsSuccessStatusCode && !r.Headers.Contains("Idempotent-Replay"));

        Assert.Equal(1, executed);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
