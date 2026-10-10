using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.StaffAi.DailyBrief;

namespace HappyPaws.UnitTests.Application.StaffAi;

/// <summary>The brief's numbers come from the reports, and money never reaches the prompt.</summary>
public class DailyBriefAssemblerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static AppointmentStatsDto Appointments() => new(
        Today, Today, "brn_1", "day",
        new AppointmentTotalsDto(Total: 14, Upcoming: 11, Arrived: 2, Completed: 0, Cancelled: 1, LateCancelled: 0, NoShow: 0, WalkIn: 3, BookedOnline: 6, null, null),
        []);

    private static QueueStatsDto Queue() => new(
        Today, Today, "brn_1",
        new QueueTotalsDto(Tickets: 4, Served: 1, Skipped: 0, Left: 0, Outstanding: 3, null, null, null),
        [], []);

    private static InventoryAlertDto Alert(string kind, string name) => new(kind, $"itm_{name}", name, "box", "brn_1", 0, 5, 0);

    private static RevenueAnalyticsDto Yesterday() => new(
        Today.AddDays(-1), Today.AddDays(-1), "brn_1", "day",
        new RevenueTotalsDto(CollectedSatang: 1_250_000, RefundedSatang: 50_000, NetSatang: 1_200_000, Payments: 21, Refunds: 1, InvoicedSatang: 1_300_000, Invoices: 22),
        [], [], []);

    [Fact]
    public void The_numbers_are_the_reports_own_totals()
    {
        var alerts = new[]
        {
            Alert("expired", "วัคซีนพิษสุนัขบ้า"), Alert("out_of_stock", "ยาถ่ายพยาธิ"), Alert("out_of_stock", "ผ้าก๊อซ"),
            Alert("near_expiry", "น้ำเกลือ"), Alert("low_stock", "เข็ม"), Alert("low_stock", "ไซริงค์"),
        };

        var numbers = DailyBriefAssembler.Numbers(Appointments(), Queue(), alerts, Yesterday());

        Assert.Equal(new BriefAppointmentsDto(14, 11, 2, 1, 3, 6), numbers.Appointments);
        Assert.Equal(new BriefQueueDto(4, 3, 1), numbers.Queue);
        Assert.Equal((1, 2, 1, 2), (numbers.Inventory.Expired, numbers.Inventory.OutOfStock, numbers.Inventory.NearExpiry, numbers.Inventory.LowStock));
        Assert.Equal(DailyBriefAssembler.TopStockItems, numbers.Inventory.Top.Count);
        Assert.Equal(new BriefStockItemDto("expired", "วัคซีนพิษสุนัขบ้า"), numbers.Inventory.Top[0]);
        Assert.Equal(new BriefRevenueDto(Today.AddDays(-1), 1_250_000, 50_000, 1_200_000, 21), numbers.YesterdayRevenue);
    }

    [Fact]
    public void The_prompt_has_every_number_but_money()
    {
        var numbers = DailyBriefAssembler.Numbers(Appointments(), Queue(), [Alert("expired", "วัคซีนพิษสุนัขบ้า")], Yesterday());

        var data = DailyBriefAssembler.PromptData("สาขาสุขุมวิท", Today, numbers);

        Assert.Contains("Branch: สาขาสุขุมวิท", data);
        Assert.Contains("total 14, still to come 11", data);
        Assert.Contains("walk-in 3", data);
        Assert.Contains("tickets 4, waiting or in progress 3", data);
        Assert.Contains("expired lots 1", data);
        Assert.Contains("- expired: วัคซีนพิษสุนัขบ้า", data);
        Assert.DoesNotContain("1250000", data);
        Assert.DoesNotContain("1200000", data);
        Assert.DoesNotContain("satang", data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void No_revenue_report_means_no_revenue_numbers()
    {
        Assert.Null(DailyBriefAssembler.Numbers(Appointments(), Queue(), [], null).YesterdayRevenue);
    }
}
