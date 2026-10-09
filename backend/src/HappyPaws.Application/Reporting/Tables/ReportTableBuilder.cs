using HappyPaws.Application.Analytics;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Common;
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
    public async Task<ReportTable> BuildAsync(ReportExport export, CancellationToken cancellationToken = default)
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

    private static ReportTable Revenue(IReadOnlyList<RevenueRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Day("วันที่"),
            ReportColumn.Text("รหัสสาขา"),
            ReportColumn.Text("สาขา"),
            ReportColumn.Text("ช่องทางชำระ"),
            ReportColumn.Baht("รับเงิน (บาท)"),
            ReportColumn.Count("จำนวนรายการรับ"),
            ReportColumn.Baht("คืนเงิน (บาท)"),
            ReportColumn.Count("จำนวนรายการคืน"),
            ReportColumn.Baht("สุทธิ (บาท)"),
        ]);

        foreach (var row in rows.OrderBy(row => row.Day).ThenBy(row => row.BranchId, StringComparer.Ordinal).ThenBy(row => row.Method))
        {
            table.Add(
                row.Day,
                row.BranchId,
                row.BranchName,
                BillingWire.ToWire(row.Method),
                ReportTable.Baht(row.CollectedSatang),
                row.Payments,
                ReportTable.Baht(row.RefundedSatang),
                row.Refunds,
                ReportTable.Baht(row.CollectedSatang - row.RefundedSatang));
        }

        return table;
    }

    private static ReportTable Services(IReadOnlyList<ServiceRevenueRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Text("รหัสบริการ"),
            ReportColumn.Text("บริการ"),
            ReportColumn.Text("ประเภทรายการ"),
            ReportColumn.Count("จำนวน"),
            ReportColumn.Count("จำนวนใบแจ้งหนี้"),
            ReportColumn.Baht("ยอดตามราคา (บาท)"),
            ReportColumn.Baht("รายได้ (บาท)"),
            ReportColumn.Baht("ค่ายาในบิลเดียวกัน (บาท)"),
            ReportColumn.Baht("รายได้หลังหักยา (บาท)"),
        ]);

        foreach (var row in rows.OrderByDescending(row => row.RevenueSatang).ThenBy(row => row.Name, StringComparer.Ordinal))
        {
            table.Add(
                row.ServiceId,
                row.Name,
                BillingWire.ToWire(row.Kind),
                row.Quantity,
                row.Invoices,
                ReportTable.Baht(row.BilledSatang),
                ReportTable.Baht(row.RevenueSatang),
                ReportTable.Baht(row.MedicineSatang),
                ReportTable.Baht(row.RevenueSatang - row.MedicineSatang));
        }

        return table;
    }

    private static ReportTable Retention(IReadOnlyList<RetentionCell> cells)
    {
        var table = new ReportTable(
        [
            ReportColumn.Month("เดือน"),
            ReportColumn.Month("เดือนที่มาครั้งแรก"),
            ReportColumn.Count("จำนวนลูกค้า"),
        ]);

        foreach (var cell in cells.OrderBy(cell => cell.Cohort).ThenBy(cell => cell.Month))
        {
            table.Add(cell.Month, cell.Cohort, cell.Customers);
        }

        return table;
    }

    private static ReportTable Staff(IReadOnlyList<StaffPerformanceRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Text("รหัสพนักงาน"),
            ReportColumn.Text("ชื่อ"),
            ReportColumn.Text("ประเภท"),
            ReportColumn.Count("เคสที่ปิด"),
            ReportColumn.Minutes("เวลาเฉลี่ยต่อเคส (นาที)"),
            ReportColumn.Count("หัตถการ"),
            ReportColumn.Baht("รายได้จากเคส (บาท)"),
            ReportColumn.Baht("รับเงินที่เคาน์เตอร์ (บาท)"),
        ]);

        foreach (var row in rows.OrderByDescending(row => row.Cases).ThenBy(row => row.DisplayName, StringComparer.Ordinal))
        {
            table.Add(
                row.StaffId,
                row.DisplayName,
                row.StaffType,
                row.Cases,
                ReportMath.AverageMinutes(row.CaseSeconds, row.Cases),
                row.Treatments,
                ReportTable.Baht(row.AttributedRevenueSatang),
                ReportTable.Baht(row.CollectedSatang));
        }

        return table;
    }

    private static ReportTable Appointments(IReadOnlyList<AppointmentCountRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Day("วันที่"),
            ReportColumn.Text("สถานะ"),
            ReportColumn.Flag("walk-in"),
            ReportColumn.Flag("ยกเลิกกระชั้นชิด"),
            ReportColumn.Flag("จองออนไลน์"),
            ReportColumn.Count("จำนวนนัด"),
        ]);

        foreach (var row in rows.OrderBy(row => row.Day).ThenBy(row => row.Status))
        {
            table.Add(row.Day, WireEnum.Of(row.Status), row.IsWalkIn, row.IsLateCancellation, row.BookedOnline, row.Count);
        }

        return table;
    }

    private static ReportTable Queue(IReadOnlyList<QueueCountRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Day("วันที่"),
            ReportColumn.Count("ชั่วโมงที่ออกบัตร"),
            ReportColumn.Text("สถานะ"),
            ReportColumn.Count("จำนวนบัตร"),
            ReportColumn.Count("ถูกเรียกแล้ว"),
            ReportColumn.Minutes("เวลารอเฉลี่ย (นาที)"),
            ReportColumn.Count("เข้าห้องตรวจ"),
            ReportColumn.Minutes("เวลาในห้องเฉลี่ย (นาที)"),
        ]);

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

    private static ReportTable Workload(IReadOnlyList<WorkloadRow> rows)
    {
        var table = new ReportTable(
        [
            ReportColumn.Text("รหัสพนักงาน"),
            ReportColumn.Text("ชื่อ"),
            ReportColumn.Count("เคสที่ปิด"),
            ReportColumn.Minutes("เวลาเฉลี่ยต่อเคส (นาที)"),
            ReportColumn.Count("เคสที่เปิดอยู่"),
            ReportColumn.Count("คิวที่รออยู่วันนี้"),
            ReportColumn.Count("นัดในช่วงนี้"),
        ]);

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
