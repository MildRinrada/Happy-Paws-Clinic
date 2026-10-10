using System.Globalization;
using System.Text;
using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Inventory.Dtos;

namespace HappyPaws.Application.StaffAi.DailyBrief;

/// <summary>Today's appointments at the branch, from <c>GET /staff/stats/appointments</c> for the day.</summary>
public sealed record BriefAppointmentsDto(int Total, int Upcoming, int Arrived, int Cancelled, int WalkIn, int BookedOnline);

/// <summary>Today's queue so far, from <c>GET /staff/stats/queue</c> for the day.</summary>
public sealed record BriefQueueDto(int Tickets, int Outstanding, int Served);

/// <summary>One stock line that needs attention: <c>kind</c> as in <c>GET /staff/inventory/alerts</c>.</summary>
public sealed record BriefStockItemDto(string Kind, string ItemName);

/// <summary>Stock needing attention, from <c>GET /staff/inventory/alerts</c>: counts by kind and the worst few.</summary>
public sealed record BriefInventoryDto(int Expired, int OutOfStock, int NearExpiry, int LowStock, IReadOnlyList<BriefStockItemDto> Top);

/// <summary>Yesterday's money, cash basis, from <c>GET /staff/analytics/revenue</c>.</summary>
public sealed record BriefRevenueDto(DateOnly Date, long CollectedSatang, long RefundedSatang, long NetSatang, int Payments);

/// <summary>
/// The numbers a morning brief is about. Computed by code from the existing reports, stored with
/// the text, and shown beside it: the model only writes the words around them.
/// </summary>
public sealed record DailyBriefNumbersDto(
    BriefAppointmentsDto Appointments,
    BriefQueueDto Queue,
    BriefInventoryDto Inventory,
    BriefRevenueDto? YesterdayRevenue);

/// <summary>
/// Builds the brief's numbers from the reports' own answers, and the lines the model reads.
/// <para>
/// <b>No money in the prompt.</b> The brief is read by everyone with <c>dashboard:read</c>, and
/// that scope is not a money scope (the dashboard shows takings only to <c>analytics:read</c> or
/// <c>invoice:write</c>). Yesterday's revenue is stored with the numbers and shown only to those
/// callers; the narrative is written without it, so it cannot leak through the words.
/// </para>
/// </summary>
public static class DailyBriefAssembler
{
    /// <summary>How many stock lines are named, worst first.</summary>
    public const int TopStockItems = 5;

    public static DailyBriefNumbersDto Numbers(
        AppointmentStatsDto appointments,
        QueueStatsDto queue,
        IReadOnlyList<InventoryAlertDto> alerts,
        RevenueAnalyticsDto? yesterday)
    {
        int Count(string kind) => alerts.Count(alert => alert.Kind == kind);

        return new DailyBriefNumbersDto(
            new BriefAppointmentsDto(
                appointments.Totals.Total,
                appointments.Totals.Upcoming,
                appointments.Totals.Arrived,
                appointments.Totals.Cancelled,
                appointments.Totals.WalkIn,
                appointments.Totals.BookedOnline),
            new BriefQueueDto(queue.Totals.Tickets, queue.Totals.Outstanding, queue.Totals.Served),
            new BriefInventoryDto(
                Count("expired"),
                Count("out_of_stock"),
                Count("near_expiry"),
                Count("low_stock"),
                [.. alerts
                    .Take(TopStockItems)
                    .Select(alert => new BriefStockItemDto(alert.Kind, alert.ItemName))]),
            yesterday is null
                ? null
                : new BriefRevenueDto(
                    yesterday.From,
                    yesterday.Totals.CollectedSatang,
                    yesterday.Totals.RefundedSatang,
                    yesterday.Totals.NetSatang,
                    yesterday.Totals.Payments));
    }

    /// <summary>What the model is given: the day, the branch and every number but money.</summary>
    public static string PromptData(string branchName, DateOnly date, DailyBriefNumbersDto numbers)
    {
        var data = new StringBuilder();
        var day = date.ToString("dddd yyyy-MM-dd", CultureInfo.InvariantCulture);

        data.AppendLine(CultureInfo.InvariantCulture, $"Branch: {branchName}");
        data.AppendLine(CultureInfo.InvariantCulture, $"Day: {day}");

        var a = numbers.Appointments;
        data.AppendLine(CultureInfo.InvariantCulture,
            $"Appointments today: total {a.Total}, still to come {a.Upcoming}, already arrived {a.Arrived}, cancelled {a.Cancelled}, walk-in {a.WalkIn}, booked online {a.BookedOnline}");

        var q = numbers.Queue;
        data.AppendLine(CultureInfo.InvariantCulture, $"Queue so far today: tickets {q.Tickets}, waiting or in progress {q.Outstanding}, served {q.Served}");

        var s = numbers.Inventory;
        data.AppendLine(CultureInfo.InvariantCulture,
            $"Stock needing attention: expired lots {s.Expired}, out of stock {s.OutOfStock}, near expiry {s.NearExpiry}, low stock {s.LowStock}");

        foreach (var item in s.Top)
        {
            data.AppendLine(CultureInfo.InvariantCulture, $"- {item.Kind}: {item.ItemName}");
        }

        return data.ToString();
    }
}
