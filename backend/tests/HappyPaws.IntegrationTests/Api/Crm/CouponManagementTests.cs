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
/// <c>docs/contract-extensions.md § 25</c>: <c>GET /staff/coupons</c> and
/// <c>POST /staff/coupons/{id}/deactivate</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CouponManagementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static object Coupon(SeededClinic clinic, string code, int? max = null) => new
    {
        code,
        name = "ลดค่าตรวจ",
        amount_satang = 5_000,
        max_redemptions = max,
        branch_id = clinic.BranchId,
    };

    private static async Task<JsonElement> ListAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/staff/coupons{query}", Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static IEnumerable<string?> Codes(JsonElement page) =>
        page.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("code").GetString());

    private static async Task<HttpResponseMessage> DeactivateAsync(HttpClient client, string couponId, string? reason = null) =>
        await client.PostAsJsonAsync($"/staff/coupons/{couponId}/deactivate", new { reason }, Token);

    [Fact]
    public async Task Listing_needs_crm_read_and_switching_off_needs_crm_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var coupon = await CreateCouponAsync(desk, Coupon(clinic, UniqueCode("L")), Token);
        using var cashier = fixture.Cashier(clinic);
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/staff/coupons", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await DeactivateAsync(reader, coupon.GetProperty("id").GetString()!)).StatusCode);
    }

    [Fact]
    public async Task The_list_shows_uses_and_status_newest_first_and_filters_by_status()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var once = UniqueCode("O");
        var open = UniqueCode("P");
        await CreateCouponAsync(desk, Coupon(clinic, once, max: 1), Token);
        await CreateCouponAsync(desk, Coupon(clinic, open), Token);
        var bill = await RaiseIdAsync(desk, clinic, Token);
        (await DiscountAsync(desk, bill, new { coupon_code = once }, Token)).EnsureSuccessStatusCode();

        var page = await ListAsync(desk, $"?branch={clinic.BranchId}&limit=100");
        var rows = page.GetProperty("data").EnumerateArray().ToList();
        var usedUp = rows.Single(row => row.GetProperty("code").GetString() == once);

        Assert.Equal(open, rows[0].GetProperty("code").GetString());
        Assert.Equal(1, usedUp.GetProperty("redeemed_count").GetInt32());
        Assert.Equal(0, usedUp.GetProperty("remaining").GetInt32());
        Assert.Equal("used_up", usedUp.GetProperty("status").GetString());
        Assert.False(rows[0].TryGetProperty("remaining", out _));

        Assert.Contains(once, Codes(await ListAsync(desk, $"?status=used_up&branch={clinic.BranchId}")));
        Assert.DoesNotContain(once, Codes(await ListAsync(desk, $"?status=active&branch={clinic.BranchId}&limit=100")));
        Assert.Contains(open, Codes(await ListAsync(desk, $"?q={open[..8].ToLowerInvariant()}")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await desk.GetAsync("/staff/coupons?status=live", Token)).StatusCode);
    }

    [Fact]
    public async Task Pages_follow_the_cursor_without_repeating_a_coupon()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var made = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            made.Add((await CreateCouponAsync(desk, Coupon(clinic, UniqueCode("C")), Token)).GetProperty("code").GetString()!);
        }

        var first = await ListAsync(desk, $"?branch={clinic.BranchId}&limit=2");
        var cursor = first.GetProperty("page").GetProperty("next_cursor").GetString();
        var second = await ListAsync(desk, $"?branch={clinic.BranchId}&limit=2&cursor={Uri.EscapeDataString(cursor!)}");

        var seen = Codes(first).Concat(Codes(second)).Where(code => made.Contains(code!)).ToList();
        Assert.Equal(3, seen.Count);
        Assert.Equal(3, seen.Distinct().Count());
    }

    [Fact]
    public async Task Branch_staff_see_their_branch_and_clinic_wide_coupons_but_not_another_branchs()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.UnboundCrmDesk(home);
        var mine = UniqueCode("M");
        var theirs = UniqueCode("T");
        var everywhere = UniqueCode("E");
        await CreateCouponAsync(owner, Coupon(home, mine), Token);
        var theirCoupon = await CreateCouponAsync(owner, Coupon(other, theirs), Token);
        var wide = await CreateCouponAsync(owner, new { code = everywhere, name = "ทุกสาขา", percent = 5 }, Token);
        using var desk = fixture.CrmDesk(home);

        var listed = Codes(await ListAsync(desk, "?limit=100")).ToList();

        Assert.Contains(mine, listed);
        Assert.Contains(everywhere, listed);
        Assert.DoesNotContain(theirs, listed);
        Assert.Equal(HttpStatusCode.Forbidden, (await desk.GetAsync($"/staff/coupons?branch={other.BranchId}", Token)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await DeactivateAsync(desk, theirCoupon.GetProperty("id").GetString()!)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await DeactivateAsync(desk, wide.GetProperty("id").GetString()!)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DeactivateAsync(owner, wide.GetProperty("id").GetString()!)).StatusCode);
    }

    [Fact]
    public async Task A_switched_off_coupon_is_refused_at_the_cash_desk_while_earlier_uses_stand()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("X");
        var coupon = await CreateCouponAsync(desk, Coupon(clinic, code), Token);
        var id = coupon.GetProperty("id").GetString()!;
        var earlier = await RaiseIdAsync(desk, clinic, Token);
        (await DiscountAsync(desk, earlier, new { coupon_code = code }, Token)).EnsureSuccessStatusCode();

        var response = await DeactivateAsync(desk, id, "โค้ดหลุดไปในกลุ่มแชต");

        response.EnsureSuccessStatusCode();
        var switchedOff = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("deactivated", switchedOff.GetProperty("status").GetString());
        Assert.Equal(clinic.StaffId, switchedOff.GetProperty("deactivated_by_staff_id").GetString());
        Assert.Equal(1, switchedOff.GetProperty("redeemed_count").GetInt32());

        var later = await RaiseIdAsync(desk, clinic, Token);
        var refused = await DiscountAsync(desk, later, new { coupon_code = code }, Token);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("coupon_deactivated", await ErrorCodeAsync(refused, Token));

        Assert.Equal(5_000, (await ReadAsync(desk, earlier, Token)).GetProperty("discount_satang").GetInt64());

        var again = await DeactivateAsync(desk, id);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("state_conflict", await ErrorCodeAsync(again, Token));
        Assert.Equal(HttpStatusCode.NotFound, (await DeactivateAsync(desk, "cpn_does_not_exist")).StatusCode);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.Equal(
            1,
            await db.Set<AuditEntry>().CountAsync(entry => entry.Action == AuditActions.CouponDeactivated && entry.ResourceId == id, Token));
    }

    [Fact]
    public async Task Switching_off_without_a_body_is_accepted()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var coupon = await CreateCouponAsync(desk, Coupon(clinic, UniqueCode("N")), Token);

        var response = await desk.PostAsync($"/staff/coupons/{coupon.GetProperty("id").GetString()}/deactivate", null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
