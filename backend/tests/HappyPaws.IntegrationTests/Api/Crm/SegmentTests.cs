using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Customers;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>POST /staff/segments</c>, "กลุ่มลูกค้าจากเงื่อนไข เช่น ไม่มา 6 เดือน". Every counting test limits
/// the group to its own seeded branch, because the suite shares one database and a clinic-wide
/// count would include every other test's customers.
/// </summary>
[Collection(nameof(ApiCollection))]
public class SegmentTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_segment_needs_crm_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var response = await CreateAsync(cashier, new { branch_id = clinic.BranchId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_segment_needs_a_condition_and_sensible_values()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.UnboundCrmDesk(clinic);

        var none = await CreateAsync(owner, new { });
        var contradictory = await CreateAsync(owner, new { inactive_days = 180, visited_within_days = 90 });
        var badTier = await CreateAsync(owner, new { tiers = new[] { "platinum" } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, none.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, contradictory.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badTier.StatusCode);
    }

    [Fact]
    public async Task Staff_bound_to_a_branch_segment_only_their_own_branch()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(home);

        var everywhere = await CreateAsync(desk, new { inactive_days = 180 });
        var elsewhere = await CreateAsync(desk, new { branch_id = other.BranchId, inactive_days = 180 });
        var own = await CreateAsync(desk, new { branch_id = home.BranchId, inactive_days = 180 });

        Assert.Equal(HttpStatusCode.Forbidden, everywhere.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, elsewhere.StatusCode);
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
    }

    [Fact]
    public async Task The_conditions_count_the_right_customers()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (catOwner, catId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var desk = fixture.CrmDesk(clinic);

        // The dog's owner came 200 days ago and holds a paid package; the cat's owner came today and paid ฿1,460.
        await SellPaidAsync(desk, clinic, Token);
        await BackdateBillsAsync(clinic.CustomerId, days: 200);

        var catBill = await RaiseAsync(desk, clinic, Token, new
        {
            customer_id = catOwner,
            pet_id = catId,
            branch_id = clinic.BranchId,
            lines = new object[] { new { kind = "service", description = "ตรวจ", quantity = 1, unit_price_satang = 146_000 } },
        });
        (await PayAsync(desk, catBill.GetProperty("id").GetString()!, 146_000, Token)).EnsureSuccessStatusCode();

        Assert.Equal(2, await CountAsync(desk, new { branch_id = clinic.BranchId }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, species = new[] { "สุนัข" } }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, inactive_days = 180 }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, visited_within_days = 30 }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, min_spend_satang = 170_000 }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, has_active_package = true }));
        Assert.Equal(1, await CountAsync(desk, new { branch_id = clinic.BranchId, has_active_package = false }));
        Assert.Equal(2, await CountAsync(desk, new { branch_id = clinic.BranchId, tiers = new[] { "member" } }));
        Assert.Equal(0, await CountAsync(desk, new { branch_id = clinic.BranchId, tiers = new[] { "silver", "gold" } }));
        Assert.Equal(0, await CountAsync(desk, new { branch_id = clinic.BranchId, species = new[] { "แมว" }, inactive_days = 180 }));
    }

    [Fact]
    public async Task Reachable_counts_only_members_who_agreed_to_marketing_and_have_LINE()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (other, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var desk = fixture.CrmDesk(clinic);
        await RaiseAsync(desk, clinic, Token);
        await RaiseAsync(desk, clinic, Token, new
        {
            customer_id = other,
            pet_id = otherPet,
            branch_id = clinic.BranchId,
            lines = new object[] { new { description = "ตรวจ", quantity = 1, unit_price_satang = 10_000 } },
        });

        await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, true, Token);
        await NotificationScenario.GiveLineAddressAsync(fixture, other, Token);

        var response = await CreateAsync(desk, new { branch_id = clinic.BranchId, visited_within_days = 30 }, "ลูกค้าเดือนนี้");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var segment = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.StartsWith("seg_", segment.GetProperty("id").GetString(), StringComparison.Ordinal);
        Assert.Equal(2, segment.GetProperty("customer_count").GetInt32());
        Assert.Equal(1, segment.GetProperty("reachable_count").GetInt32());
        Assert.Equal(30, segment.GetProperty("criteria").GetProperty("visited_within_days").GetInt32());
    }

    private async Task<HttpResponseMessage> CreateAsync(HttpClient staff, object criteria, string name = "กลุ่มทดสอบ") =>
        await staff.PostAsJsonAsync("/staff/segments", new { name, criteria }, Token);

    private async Task<int> CountAsync(HttpClient staff, object criteria)
    {
        var response = await CreateAsync(staff, criteria);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("customer_count").GetInt32();
    }

    /// <summary>Moves a customer's bills into the past, so "last visit" is long ago.</summary>
    private async Task BackdateBillsAsync(string customerId, int days)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var past = DateTimeOffset.UtcNow.AddDays(-days);

        await db.Set<Invoice>()
            .Where(invoice => invoice.CustomerId == customerId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invoice => invoice.IssuedAt, past), Token);
    }
}
