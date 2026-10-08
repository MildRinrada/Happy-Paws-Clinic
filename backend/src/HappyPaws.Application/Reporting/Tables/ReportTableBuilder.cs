using HappyPaws.Application.Analytics;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Reporting.Csv;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.Tables;

/// <summary>
/// Builds the table of one export from the same read models the dashboard uses
/// (<see cref="IBusinessAnalyticsQueries"/>, <see cref="IOperationalStatsQueries"/>), so a file and
/// its screen cannot count differently.
/// <para>
/// The rows are the queries' own grouped rows, one per day, branch and tender for revenue and so on,
/// rather than the screen's folded totals: a spreadsheet sums and pivots for itself, and the
/// finest grain is what makes that possible.
/// </para>
/// <para>
/// The window is rebuilt from what the export stored at request time, never from a caller: the
/// worker runs with nobody signed in, and the branch guard would read "nobody" as "everything".
/// Headers are in Thai for the clinic; codes (statuses, tenders) stay as the API writes them so a
/// file can be matched with the API's own answers.
/// </para>
/// </summary>
public sealed class ReportTableBuilder(
    IBusinessAnalyticsQueries business,
    IOperationalStatsQueries operational,
    ClinicDays days)
{
    public async Task<CsvTable> BuildAsync(ReportExport export, CancellationToken cancellationToken = default)
    {
        var window = new ReportWindow(
            export.FromDate,
            export.ToDate,
            days.StartOf(export.FromDate),
            days.StartOf(export.ToDate.AddDays(1)),
            export.BranchId,
            export.AllBranches ? null : export.BranchIds);

        return export.Report switch
        {
            ReportKind.Revenue => Revenue(await business.RevenueAsync(window, cancellationToken)),
            ReportKind.Services => Services(await business.ServiceRevenueAsync(window, cancellationToken)),
            ReportKind.Retention => Retention(await business.RetentionAsync(window, cancellationToken)),
            ReportKind.StaffPerformance => Staff(await business.StaffPerformanceAsync(window, cancellationToken)),
            ReportKind.Appointments => Appointments(await operational.AppointmentCountsAsync(window, cancellationToken)),
            ReportKind.Queue => Queue(await operational.QueueCountsAsync(window, cancellationToken)),
            _ => Workload(await operational.WorkloadAsync(window, days.Today, cancellationToken)),
        };
    }

    private static CsvTable Revenue(IReadOnlyList<RevenueRow> rows)
    {
        var table = new CsvTable(
            ["วันที่", "รหัสสาขา", "สาขา", "ช่องทางชำระ", "รับเงิน (บาท)", "จำนวนรายการรับ", "คืนเงิน (บาท)", "จำนวนรายการคืน", "สุทธิ (บาท)"]);

        foreach (var row in rows.OrderBy(row => row.Day).ThenBy(row => row.BranchId, StringComparer.Ordinal).ThenBy(row => row.Method))
        {
            table.Add(
                row.Day,
                row.BranchId,
                row.BranchName,
                BillingWire.ToWire(row.Method),
                CsvTable.Baht(row.CollectedSatang),
                row.Payments,
                CsvTable.Baht(row.RefundedSatang),
                row.Refunds,
                CsvTable.Baht(row.CollectedSatang - row.RefundedSatang));
        }

        return table;
    }

    private static CsvTable Services(IReadOnlyList<ServiceRevenueRow> rows)
    {
        var table = new CsvTable(
            ["รหัสบริการ", "บริการ", "ประเภทรายการ", "จำนวน", "จำนวนใบแจ้งหนี้", "ยอดตามราคา (บาท)", "รายได้ (บาท)", "ค่ายาในบิลเดียวกัน (บาท)", "รายได้หลังหักยา (บาท)"]);

        foreach (var row in rows.OrderByDescending(row => row.RevenueSatang).ThenBy(row => row.Name, StringComparer.Ordinal))
        {
            table.Add(
                row.ServiceId,
                row.Name,
                BillingWire.ToWire(row.Kind),
                row.Quantity,
                row.Invoices,
                CsvTable.Baht(row.BilledSatang),
                CsvTable.Baht(row.RevenueSatang),
                CsvTable.Baht(row.MedicineSatang),
                CsvTable.Baht(row.RevenueSatang - row.MedicineSatang));
        }

        return table;
    }

    private static CsvTable Retention(IReadOnlyList<RetentionCell> cells)
    {
        var table = new CsvTable(["เดือน", "เดือนที่มาครั้งแรก", "จำนวนลูกค้า"]);

        foreach (var cell in cells.OrderBy(cell => cell.Cohort).ThenBy(cell => cell.Month))
        {
            table.Add(cell.Month, cell.Cohort, cell.Customers);
        }

        return table;
    }

    private static CsvTable Staff(IReadOnlyList<StaffPerformanceRow> rows)
    {
        var table = new CsvTable(
            ["รหัสพนักงาน", "ชื่อ", "ประเภท", "เคสที่ปิด", "เวลาเฉลี่ยต่อเคส (นาที)", "หัตถการ", "รายได้จากเคส (บาท)", "รับเงินที่เคาน์เตอร์ (บาท)"]);

        foreach (var row in rows.OrderByDescending(row => row.Cases).ThenBy(row => row.DisplayName, StringComparer.Ordinal))
        {
            table.Add(
                row.StaffId,
                row.DisplayName,
                row.StaffType,
                row.Cases,
                ReportMath.AverageMinutes(row.CaseSeconds, row.Cases),
                row.Treatments,
                CsvTable.Baht(row.AttributedRevenueSatang),
                CsvTable.Baht(row.CollectedSatang));
        }

        return table;
    }

    private static CsvTable Appointments(IReadOnlyList<AppointmentCountRow> rows)
    {
        var table = new CsvTable(["วันที่", "สถานะ", "walk-in", "ยกเลิกกระชั้นชิด", "จองออนไลน์", "จำนวนนัด"]);

        foreach (var row in rows.OrderBy(row => row.Day).ThenBy(row => row.Status))
        {
            table.Add(row.Day, WireEnum.Of(row.Status), row.IsWalkIn, row.IsLateCancellation, row.BookedOnline, row.Count);
        }

        return table;
    }

    private static CsvTable Queue(IReadOnlyList<QueueCountRow> rows)
    {
        var table = new CsvTable(
            ["วันที่", "ชั่วโมงที่ออกบัตร", "สถานะ", "จำนวนบัตร", "ถูกเรียกแล้ว", "เวลารอเฉลี่ย (นาที)", "เข้าห้องตรวจ", "เวลาในห้องเฉลี่ย (นาที)"]);

        foreach (var row in rows.OrderBy(row => row.Day).ThenBy(row => row.Hour).ThenBy(row => row.Status))
        {
            table.Add(
                row.Day,
                row.Hour,
                WireEnum.Of(row.Status),
                row.Tickets,
                row.Called,
                ReportMath.AverageMinutes(row.WaitSeconds, row.Called),
                row.Roomed,
                ReportMath.AverageMinutes(row.RoomSeconds, row.Roomed));
        }

        return table;
    }

    private static CsvTable Workload(IReadOnlyList<WorkloadRow> rows)
    {
        var table = new CsvTable(
            ["รหัสพนักงาน", "ชื่อ", "เคสที่ปิด", "เวลาเฉลี่ยต่อเคส (นาที)", "เคสที่เปิดอยู่", "คิวที่รออยู่วันนี้", "นัดในช่วงนี้"]);

        foreach (var row in rows.OrderByDescending(row => row.Cases).ThenBy(row => row.DisplayName, StringComparer.Ordinal))
        {
            table.Add(
                row.StaffId,
                row.StaffId is null ? "ยังไม่ได้มอบหมาย" : row.DisplayName,
                row.Cases,
                ReportMath.AverageMinutes(row.CaseSeconds, row.Cases),
                row.OpenCases,
                row.WaitingQueue,
                row.Appointments);
        }

        return table;
    }
}
