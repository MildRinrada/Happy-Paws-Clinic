using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>A customer as the notification tests need one: how to reach them.</summary>
public sealed record Reachable(string CustomerId, string PhoneDigits, string? LineUserId, string? Email);

/// <summary>Customers with addresses, consent, the LINE settings, and reading back the delivery log.</summary>
public static class NotificationScenario
{
    public const string ChannelSecret = "0123456789abcdef0123456789abcdef";
    public const string AccessToken = "test-channel-access-token";

    public static readonly string[] AllNotifyScopes =
    [
        Scopes.Notify.Send, Scopes.Notify.Read, Scopes.Notify.Manage, Scopes.Notify.Broadcast, Scopes.Integration.Manage,
    ];

    public static HttpClient Notifier(this ApiFixture fixture, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: scopes.Length == 0 ? AllNotifyScopes : scopes));

    /// <summary>A customer with a phone, and a LINE address and an email when asked for.</summary>
    public static async Task<Reachable> CustomerAsync(
        ApiFixture fixture,
        bool line = true,
        bool email = false,
        CancellationToken cancellationToken = default)
    {
        var digits = $"08{Random.Shared.Next(10_000_000, 99_999_999)}";
        var address = email ? $"owner-{Guid.NewGuid():N}@mail.test" : null;
        var lineUserId = line ? $"U{Guid.NewGuid():N}" : null;

        var customer = Customer.Create("คุณลูกค้า แจ้งเตือน", digits, address);
        customer.SetLineAddress(lineUserId);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return new Reachable(customer.Id, digits, lineUserId, address);
    }

    /// <summary>Gives an existing customer (one seeded by <c>ClinicSeed</c>) a LINE address.</summary>
    public static async Task<string> GiveLineAddressAsync(ApiFixture fixture, string customerId, CancellationToken cancellationToken = default)
    {
        var lineUserId = $"U{Guid.NewGuid():N}";

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var customer = await db.Set<Customer>().FirstAsync(item => item.Id == customerId, cancellationToken);
        customer.SetLineAddress(lineUserId);
        await db.SaveChangesAsync(cancellationToken);

        return lineUserId;
    }

    /// <summary>Records a consent decision at the counter, as staff would.</summary>
    public static async Task ConsentAsync(
        ApiFixture fixture,
        string customerId,
        ConsentPurpose purpose,
        bool granted,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<CustomerConsent>().Add(CustomerConsent.Record(
            customerId, purpose, granted, ConsentSource.Counter, DateTimeOffset.UtcNow, recordedByUserId: "usr_test"));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>LINE set up through the API with a secret and a token, switched on, every type on.</summary>
    public static async Task<JsonElement> ConfigureLineAsync(
        ApiFixture fixture,
        object? overrides = null,
        CancellationToken cancellationToken = default)
    {
        using var client = fixture.Notifier(Scopes.Integration.Manage);

        var response = await client.PutAsJsonAsync(
            "/staff/integrations/line",
            overrides ?? new
            {
                channel_id = "1650000000",
                channel_secret = ChannelSecret,
                channel_access_token = AccessToken,
                enabled = true,
                notification_types = new Dictionary<string, bool>
                {
                    ["queue.called"] = true,
                    ["appointment.reminder"] = true,
                    ["receipt.issued"] = true,
                    ["vaccine.due"] = true,
                    ["clinic.message"] = true,
                    ["clinic.announcement"] = true,
                },
                broadcast_monthly_limit = 300,
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Every delivery-log row for one customer, oldest first.</summary>
    public static async Task<IReadOnlyList<NotificationDelivery>> DeliveriesAsync(
        ApiFixture fixture,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<NotificationDelivery>().AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public static async Task<Customer> ReloadCustomerAsync(ApiFixture fixture, string customerId, CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Customer>().AsNoTracking().FirstAsync(item => item.Id == customerId, cancellationToken);
    }
}
