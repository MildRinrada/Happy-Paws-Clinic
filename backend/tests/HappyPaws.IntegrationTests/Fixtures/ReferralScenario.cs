using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Refer a friend (<c>docs/contract-extensions.md § 39</c>): switching the programme on, reading a
/// customer's code, a newcomer booking through <c>/book</c> with it, and paying their first bill.
/// </summary>
public static class ReferralScenario
{
    private const string SettingsPath = "/clinic/settings/loyalty";

    private static HttpClient Admin(ApiFixture fixture) =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Read, Scopes.Settings.Write]));

    /// <summary>
    /// Replaces the programme with the default points rules and these referral values. Sent whole,
    /// so a test reads back exactly what it set.
    /// </summary>
    public static async Task<JsonElement> ConfigureAsync(
        ApiFixture fixture,
        bool enabled = true,
        int referrerPoints = 100,
        int friendPoints = 100,
        int monthlyCap = 5,
        CancellationToken cancellationToken = default)
    {
        using var admin = Admin(fixture);
        var current = await admin.GetFromJsonAsync<JsonElement>(SettingsPath, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Put, SettingsPath)
        {
            Content = JsonContent.Create(new
            {
                satang_per_point = 10_000,
                point_value_satang = 100,
                silver_min_points = 200,
                gold_min_points = 1_000,
                referral_enabled = enabled,
                referral_referrer_points = referrerPoints,
                referral_friend_points = friendPoints,
                referral_monthly_cap = monthlyCap,
            }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{current.GetProperty("version").GetUInt32()}\"");

        var response = await admin.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.IsSuccessStatusCode, text);

        return JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>The seeded customer's own referral page.</summary>
    public static async Task<JsonElement> MyReferralAsync(ApiFixture fixture, SeededClinic clinic, CancellationToken cancellationToken)
    {
        using var customer = fixture.CustomerOf(clinic);

        return await customer.GetFromJsonAsync<JsonElement>("/portal/referral", cancellationToken);
    }

    public static string NewPhone() => $"06{Random.Shared.Next(10_000_000, 99_999_999)}";

    /// <summary>
    /// A visitor books through the website with a referral code: holds a time, proves the phone,
    /// and comes back signed in. Answers the verify body (with the new customer's id under
    /// <c>capabilities</c>).
    /// </summary>
    public static async Task<JsonElement> BookAsGuestAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        string phone,
        string? referralCode,
        int hoursFromNow,
        CancellationToken cancellationToken)
    {
        using var client = fixture.CreateClient();

        var started = await client.PostAsJsonAsync("/booking/guest/start", new
        {
            phone,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
            starts_at = ClinicSeed.SlotAt(hoursFromNow),
        }, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, started.StatusCode);

        var hold = (await started.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("hold");
        var code = fixture.SignIn.CodeFor(phone) ?? throw new InvalidOperationException("no code was sent");

        var verified = await client.PostAsJsonAsync("/booking/guest/verify", new
        {
            hold_id = hold.GetProperty("id").GetString(),
            phone,
            code,
            display_name = "คุณเพื่อน ที่ถูกชวน",
            accept_privacy_notice = true,
            marketing_consent = false,
            referral_code = referralCode,
        }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);

        return await verified.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public static string CustomerIdOf(JsonElement verified) =>
        verified.GetProperty("capabilities").GetProperty("customer_id").GetString()!;

    /// <summary>Raises a ฿500 counter bill for a customer at the seeded branch and pays it in full. Answers the bill id.</summary>
    public static async Task<string> PayBillAsync(
        HttpClient cashier,
        SeededClinic clinic,
        string customerId,
        CancellationToken cancellationToken,
        long amountSatang = 50_000)
    {
        var bill = await BillingScenario.RaiseAsync(cashier, clinic, cancellationToken, new
        {
            customer_id = customerId,
            branch_id = clinic.BranchId,
            lines = new object[]
            {
                new { kind = "other", description = "ค่าตรวจ", quantity = 1, unit_price_satang = amountSatang },
            },
        });
        var id = bill.GetProperty("id").GetString()!;

        var paid = await BillingScenario.PayAsync(cashier, id, amountSatang, cancellationToken);
        Assert.True(paid.IsSuccessStatusCode, await paid.Content.ReadAsStringAsync(cancellationToken));

        return id;
    }

    /// <summary>The referrals where this customer is either side, as staff with <c>crm:read</c> see them.</summary>
    public static async Task<JsonElement[]> ReferralsOfAsync(HttpClient staff, string customerId, CancellationToken cancellationToken)
    {
        var page = await staff.GetFromJsonAsync<JsonElement>($"/staff/referrals?customer={customerId}", cancellationToken);

        return [.. page.GetProperty("data").EnumerateArray()];
    }

    /// <summary>The referral rows of a customer's points ledger.</summary>
    public static async Task<JsonElement[]> ReferralPointsAsync(HttpClient staff, string customerId, CancellationToken cancellationToken)
    {
        var account = await CrmScenario.LoyaltyAsync(staff, customerId, cancellationToken);

        return [.. account.GetProperty("history").EnumerateArray().Where(entry => entry.GetProperty("kind").GetString() == "referral")];
    }
}

/// <summary>
/// Refer a friend in a database of its own: the programme switch and the cap are one settings row
/// shared by every test in a database, and these tests turn it on and change it.
/// </summary>
[CollectionDefinition(nameof(ReferralCollection))]
public sealed class ReferralCollection : ICollectionFixture<ApiFixture>;

/// <summary>A deployment whose public referral code check allows two a minute per address, in its own host.</summary>
public sealed class ReferralCheckLimitFixture : ApiFixture
{
    public const int PerMinute = 2;

    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["RateLimits:ReferralChecksPerIpPerMinute"] = PerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture) };
}

[CollectionDefinition(nameof(ReferralCheckLimitCollection))]
public sealed class ReferralCheckLimitCollection : ICollectionFixture<ReferralCheckLimitFixture>;
