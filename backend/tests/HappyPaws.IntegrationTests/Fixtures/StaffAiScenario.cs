using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HappyPaws.Domain.Access;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>A person whose details must never reach the model.</summary>
public sealed record OwnerDetails(string Name, string Phone, string Email);

/// <summary>Helpers for the staff AI assistant's tests (<c>docs/contract-extensions.md § 34</c>).</summary>
public static class StaffAiScenario
{
    /// <summary>A vet who may use the assistant everywhere it reaches: records, cases, messages.</summary>
    public static readonly string[] AiVetScopes =
    [
        .. ConsultationScenario.ClinicalScopes,
        Scopes.Ai.Use,
        Scopes.Message.Read,
        Scopes.Message.Write,
        Scopes.Dashboard.Read,
    ];

    public static HttpClient AiVet(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Vet(clinic, scopes.Length == 0 ? AiVetScopes : scopes);

    /// <summary>
    /// Gives the seeded customer a name and an email of their own, so a test can look for exactly
    /// those strings in what the model was sent. Returns the customer's details as stored.
    /// </summary>
    public static async Task<OwnerDetails> DistinctOwnerAsync(ApiFixture fixture, string customerId, CancellationToken cancellationToken)
    {
        const string name = "คุณวิมลรัตน์ ศรีสวัสดิ์";
        var email = $"wimon-{Guid.NewGuid():N}@example.com";

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE customers SET display_name = {name}, email = {email} WHERE id = {customerId}",
            cancellationToken);

        var phone = await db.Database
            .SqlQuery<string>($"SELECT phone AS \"Value\" FROM customers WHERE id = {customerId}")
            .SingleAsync(cancellationToken);

        return new OwnerDetails(name, phone, email);
    }

    /// <summary>Everything the model server was sent, as readable text (Thai not escaped).</summary>
    public static string SentToModel(this ApiFixture fixture) =>
        string.Join("\n", fixture.Model.Requests.Select(request => request.ToJsonString(Readable)));

    /// <summary>The names of the tools declared in one request to the model.</summary>
    public static IReadOnlyList<string> ToolsOffered(JsonNode request) =>
        request["tools"] is JsonArray tools
            ? [.. tools.Select(tool => tool!["function"]!["name"]!.GetValue<string>())]
            : [];

    /// <summary>The user message of a single-turn request: the fenced data.</summary>
    public static string UserMessage(JsonNode request) =>
        request["messages"]!.AsArray().Last(message => message!["role"]!.GetValue<string>() == "user")!["content"]!.GetValue<string>();

    /// <summary>Current use of the month, from <c>GET /staff/ai/usage</c>.</summary>
    public static async Task<JsonElement> UsageAsync(HttpClient staff, CancellationToken cancellationToken) =>
        await staff.GetFromJsonAsync<JsonElement>("/staff/ai/usage", cancellationToken);

    private static readonly JsonSerializerOptions Readable = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}

/// <summary>
/// A deployment whose monthly AI allowance is three uses, in its own database: the counter is
/// clinic-wide, so the limit tests cannot share it with anything that spends uses.
/// </summary>
public sealed class StaffAiLimitFixture : ApiFixture
{
    public const int MonthlyCalls = 3;

    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["StaffAi:MonthlyCalls"] = MonthlyCalls.ToString(System.Globalization.CultureInfo.InvariantCulture) };
}

[CollectionDefinition(nameof(StaffAiLimitCollection))]
public sealed class StaffAiLimitCollection : ICollectionFixture<StaffAiLimitFixture>;

/// <summary>
/// A deployment that lets one person call the model twice a minute, in its own host: the shared
/// fixture lifts the limit so ordinary tests can call freely.
/// </summary>
public sealed class StaffAiRateLimitFixture : ApiFixture
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["StaffAi:CallsPerMinutePerUser"] = "2" };
}

[CollectionDefinition(nameof(StaffAiRateLimitCollection))]
public sealed class StaffAiRateLimitCollection : ICollectionFixture<StaffAiRateLimitFixture>;

/// <summary>A deployment that switched the staff assistant off while the website assistant stays on.</summary>
public sealed class StaffAiOffFixture : ApiFixture
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["StaffAi:Provider"] = "off" };
}

[CollectionDefinition(nameof(StaffAiOffCollection))]
public sealed class StaffAiOffCollection : ICollectionFixture<StaffAiOffFixture>;

/// <summary>
/// The morning brief job, in its own database: a run writes a brief for every branch there is, and
/// in the shared database that would be every other test's branches too.
/// </summary>
[CollectionDefinition(nameof(StaffAiBriefCollection))]
public sealed class StaffAiBriefCollection : ICollectionFixture<ApiFixture>;
