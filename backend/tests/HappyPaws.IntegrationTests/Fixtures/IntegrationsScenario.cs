using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>Helpers for the "Webhooks and Integrations" tests.</summary>
public static class IntegrationsScenario
{
    /// <summary>A unique receiver host per test, so one test's deliveries never count in another's.</summary>
    public static string Host() => $"hooks-{Guid.NewGuid():N}.example.com";

    /// <summary>A member of staff at the clinic's branch, holding <c>integration:manage</c> and whatever else is passed.</summary>
    public static HttpClient Integrator(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [Scopes.Integration.Manage, .. scopes],
            [clinic.BranchId]));

    public static async Task<JsonElement> CreateWebhookAsync(
        HttpClient client,
        string host,
        IReadOnlyList<string> events,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/staff/webhooks",
            new { url = $"https://{host}/hooks/clinic", events, description = "ทดสอบ" },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Outbox rows of one type whose body mentions <paramref name="id"/>.</summary>
    public static async Task<IReadOnlyList<WebhookEvent>> EventsAboutAsync(
        ApiFixture fixture,
        string type,
        string id,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<WebhookEvent>().AsNoTracking()
            .Where(webhookEvent => webhookEvent.Type == type && webhookEvent.Body.Contains(id))
            .ToListAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<WebhookDelivery>> DeliveriesAsync(
        ApiFixture fixture,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<WebhookDelivery>().AsNoTracking()
            .Where(delivery => delivery.SubscriptionId == subscriptionId)
            .OrderBy(delivery => delivery.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Makes a pending delivery due now, standing in for the wait until its next attempt.</summary>
    public static async Task MakeDueAsync(ApiFixture fixture, string deliveryId, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        await db.Set<WebhookDelivery>()
            .Where(delivery => delivery.Id == deliveryId)
            .ExecuteUpdateAsync(set => set.SetProperty(delivery => delivery.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)), cancellationToken);
    }

    /// <summary>Creates a customer through the API; the cheapest business event there is.</summary>
    public static async Task<string> CreateCustomerAsync(HttpClient client, CancellationToken cancellationToken, string name = "คุณสมชาย ทดสอบ")
    {
        var response = await client.PostAsJsonAsync(
            "/staff/customers",
            new { display_name = name, phone = $"08{Random.Shared.Next(10_000_000, 99_999_999)}", allow_duplicate_phone = true },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("id").GetString()!;
    }

    public static async Task<string> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("error").GetProperty("code").GetString()!;
}
