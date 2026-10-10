using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A clinic of its own for publishing the privacy notice (<c>docs/contract-extensions.md § 35</c>).
/// <para>
/// "The current notice" is one per instance and every consent is stamped with it, so a test that
/// published one in <see cref="ApiCollection"/> would change what two hundred other tests record.
/// Nothing is ever published in <see cref="ApiCollection"/>, which is also what lets the tests
/// there check the state before the first publish.
/// </para>
/// </summary>
[CollectionDefinition(nameof(PrivacyPolicyCollection))]
public sealed class PrivacyPolicyCollection : ICollectionFixture<ApiFixture>;

/// <summary>Drafting and publishing the notice through the API, as the settings page does.</summary>
public static class PrivacyPolicyScenario
{
    public const string Versions = "/staff/privacy-policy/versions";

    public static HttpClient Writer(this ApiFixture fixture) =>
        fixture.Authenticated(TestTokens.Staff(fixture, staffId: "stf_writer", scopes: [Scopes.Settings.Read, Scopes.Settings.Write]));

    public static HttpClient Owner(this ApiFixture fixture) =>
        fixture.Authenticated(TestTokens.Staff(fixture, staffId: "stf_owner", scopes: [Scopes.Wildcard]));

    public static string UniqueLabel(string stem = "t") => $"{stem}-{Guid.NewGuid().ToString("N")[..10]}";

    /// <summary>Today in clinic time, the earliest effective date a publish accepts.</summary>
    public static string Today() => ClinicSeed.ClinicDate(DateTimeOffset.UtcNow);

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task<JsonElement> ErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("error");

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body,
        uint? ifMatch,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return await client.SendAsync(request, cancellationToken);
    }

    public static async Task<JsonElement> CreateDraftAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string source = "template",
        string? versionId = null)
    {
        var response = await client.PostAsJsonAsync(Versions, new { source, version_id = versionId }, cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        return await ReadAsync(response, cancellationToken);
    }

    public static Task<HttpResponseMessage> PublishAsync(
        HttpClient client,
        JsonElement draft,
        string label,
        CancellationToken cancellationToken,
        string? effectiveDate = null,
        bool legalReviewDone = false,
        bool sendVersion = true) =>
        SendAsync(
            client,
            HttpMethod.Post,
            $"{Versions}/{IdOf(draft)}/publish",
            new { label, effective_date = effectiveDate ?? Today(), legal_review_done = legalReviewDone },
            sendVersion ? VersionOf(draft) : null,
            cancellationToken);

    public static async Task<JsonElement> PublishedAsync(
        HttpClient owner,
        string label,
        CancellationToken cancellationToken,
        string source = "template")
    {
        var draft = await CreateDraftAsync(owner, cancellationToken, source);

        return await ReadAsync(await PublishAsync(owner, draft, label, cancellationToken), cancellationToken);
    }

    public static string IdOf(JsonElement version) => version.GetProperty("id").GetString()!;

    public static uint VersionOf(JsonElement version) => version.GetProperty("version").GetUInt32();
}
