using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: <c>GET</c> and <c>PUT /clinic/settings/loyalty</c>. The
/// suite shares one database and one settings row, so every test that changes it puts the defaults
/// back.
/// </summary>
[Collection(nameof(ApiCollection))]
public class LoyaltySettingsTests(ApiFixture fixture)
{
    private const string Path = "/clinic/settings/loyalty";

    private static readonly object Defaults = new
    {
        satang_per_point = 10_000,
        point_value_satang = 100,
        silver_min_points = 200,
        gold_min_points = 1_000,
        points_expire_after_months = (int?)null,
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    private async Task<JsonElement> PutAsync(object body)
    {
        using var admin = Admin();
        var current = await admin.GetFromJsonAsync<JsonElement>(Path, Token);

        using var request = new HttpRequestMessage(HttpMethod.Put, Path) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{current.GetProperty("version").GetUInt32()}\"");

        var response = await admin.SendAsync(request, Token);
        var text = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, text);

        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task RestoreAsync() => await PutAsync(Defaults);

    [Fact]
    public async Task Reading_needs_settings_read_and_changing_needs_settings_write()
    {
        using var reader = Admin(Scopes.Settings.Read);
        using var promoter = Admin(Scopes.Crm.Manage, Scopes.Crm.Read);

        var settings = await reader.GetFromJsonAsync<JsonElement>(Path, Token);

        Assert.Equal(10_000, settings.GetProperty("satang_per_point").GetInt64());
        Assert.Equal(HttpStatusCode.Forbidden, (await promoter.GetAsync(Path, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PutAsJsonAsync(Path, Defaults, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await promoter.PutAsJsonAsync(Path, Defaults, Token)).StatusCode);
    }

    [Theory]
    [InlineData(10_000, 100, 500, 500, null, "gold_min_points")]
    [InlineData(50, 100, 200, 1_000, null, "satang_per_point")]
    [InlineData(10_000, 0, 200, 1_000, null, "point_value_satang")]
    [InlineData(10_000, 100, 200, 1_000, 0, "points_expire_after_months")]
    public async Task A_programme_outside_its_bounds_is_a_validation_error(
        long rate, long value, int silver, int gold, int? months, string field)
    {
        using var admin = Admin();

        var response = await admin.PutAsJsonAsync(Path, new
        {
            satang_per_point = rate,
            point_value_satang = value,
            silver_min_points = silver,
            gold_min_points = gold,
            points_expire_after_months = months,
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(field, body.GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_stale_version_does_not_overwrite_someone_elses_change()
    {
        using var admin = Admin();
        var before = await admin.GetFromJsonAsync<JsonElement>(Path, Token);

        try
        {
            await PutAsync(new
            {
                satang_per_point = 10_000,
                point_value_satang = 100,
                silver_min_points = 250,
                gold_min_points = 1_000,
                points_expire_after_months = (int?)null,
            });

            using var stale = new HttpRequestMessage(HttpMethod.Put, Path) { Content = JsonContent.Create(Defaults) };
            stale.Headers.TryAddWithoutValidation("If-Match", $"\"{before.GetProperty("version").GetUInt32()}\"");
            var response = await admin.SendAsync(stale, Token);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("version_conflict", await ErrorCodeAsync(response, Token));
        }
        finally
        {
            await RestoreAsync();
        }
    }

    [Fact]
    public async Task A_new_programme_applies_to_earning_and_redeeming_from_now_on_and_never_rewrites_the_ledger()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var before = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, before, 146_000, Token)).EnsureSuccessStatusCode();

        try
        {
            var saved = await PutAsync(new
            {
                satang_per_point = 5_000,
                point_value_satang = 200,
                silver_min_points = 20,
                gold_min_points = 100,
                points_expire_after_months = (int?)null,
            });
            Assert.Equal(5_000, saved.GetProperty("satang_per_point").GetInt64());

            var after = await RaiseIdAsync(desk, clinic, Token);
            (await PayAsync(desk, after, 146_000, Token)).EnsureSuccessStatusCode();

            var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
            var earned = account.GetProperty("history").EnumerateArray()
                .Where(row => row.GetProperty("kind").GetString() == "earn")
                .ToDictionary(row => row.GetProperty("invoice_id").GetString()!, row => row.GetProperty("points").GetInt32());

            // The bill paid before the change keeps its 14; the one after earns 29 at ฿50 a point.
            Assert.Equal(14, earned[before]);
            Assert.Equal(29, earned[after]);
            Assert.Equal("silver", account.GetProperty("tier").GetString());
            Assert.Equal(200, account.GetProperty("rules").GetProperty("point_value_satang").GetInt64());

            // A refund of the old bill takes back points at the rate they were earned at: ฿960 kept is 9, not 19.
            using (var refund = BookingScenario.Idempotent($"/staff/invoices/{before}/refunds", new { amount_satang = 50_000, reason = "คืนเงินบางส่วน" }))
            {
                (await desk.SendAsync(refund, Token)).EnsureSuccessStatusCode();
            }

            var refunded = await LoyaltyAsync(desk, clinic.CustomerId, Token);
            var reverse = refunded.GetProperty("history").EnumerateArray().Single(row => row.GetProperty("kind").GetString() == "reverse");
            Assert.Equal(-5, reverse.GetProperty("points").GetInt32());

            // Redeeming values a point at today's ฿2.
            var bill = await RaiseIdAsync(desk, clinic, Token);
            var discounted = await DiscountAsync(desk, bill, new { points = 10 }, Token);
            discounted.EnsureSuccessStatusCode();
            Assert.Equal(2_000, (await discounted.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("discount_satang").GetInt64());

            using var scope = fixture.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            Assert.True(await db.Set<AuditEntry>().AnyAsync(entry => entry.Action == AuditActions.LoyaltySettingsChanged, Token));
        }
        finally
        {
            await RestoreAsync();
        }
    }

    [Fact]
    public async Task Points_given_while_expiry_is_on_carry_their_expiry_and_points_given_before_do_not()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var before = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, before, 146_000, Token)).EnsureSuccessStatusCode();

        try
        {
            await PutAsync(new
            {
                satang_per_point = 10_000,
                point_value_satang = 100,
                silver_min_points = 200,
                gold_min_points = 1_000,
                points_expire_after_months = 6,
            });

            var after = await RaiseIdAsync(desk, clinic, Token);
            (await PayAsync(desk, after, 146_000, Token)).EnsureSuccessStatusCode();

            var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
            var rows = account.GetProperty("history").EnumerateArray()
                .ToDictionary(row => row.GetProperty("invoice_id").GetString()!);

            Assert.False(rows[before].TryGetProperty("expires_at", out _));
            var expiresAt = rows[after].GetProperty("expires_at").GetDateTimeOffset();
            Assert.InRange(expiresAt, DateTimeOffset.UtcNow.AddMonths(6).AddDays(-1), DateTimeOffset.UtcNow.AddMonths(6).AddDays(1));
            Assert.Equal(28, account.GetProperty("points_balance").GetInt32());
            Assert.Equal(14, account.GetProperty("next_expiry").GetProperty("points").GetInt32());
            Assert.Equal(6, account.GetProperty("rules").GetProperty("points_expire_after_months").GetInt32());
        }
        finally
        {
            await RestoreAsync();
        }
    }
}
