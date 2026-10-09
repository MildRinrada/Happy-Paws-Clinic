namespace HappyPaws.Domain.Reporting;

/// <summary>
/// The reports that can be exported. Each is the row data behind one dashboard read, so an export
/// and the screen it came from count the same things.
/// </summary>
public enum ReportKind
{
    /// <summary>Money in and out by clinic day, branch and tender (<c>/staff/analytics/revenue</c>).</summary>
    Revenue,

    /// <summary>Revenue per service and line kind (<c>/staff/analytics/services</c>).</summary>
    Services,

    /// <summary>Visiting customers per month by first-visit cohort (<c>/staff/analytics/retention</c>).</summary>
    Retention,

    /// <summary>Cases, treatments and money per person (<c>/staff/analytics/staff-performance</c>).</summary>
    StaffPerformance,

    /// <summary>Appointments by day and outcome (<c>/staff/stats/appointments</c>).</summary>
    Appointments,

    /// <summary>Queue tickets by day, hour and status (<c>/staff/stats/queue</c>).</summary>
    Queue,

    /// <summary>Each vet's cases and bookings (<c>/staff/stats/workload</c>).</summary>
    Workload,
}

/// <summary>
/// The file an export produces. The contract names no format; <c>xlsx</c> and <c>pdf</c> were
/// asked for by the clinic on top of the original CSV (<c>docs/contract-extensions.md § 21</c>).
/// Stored by name, so adding one needs no migration.
/// </summary>
public enum ReportFormat
{
    /// <summary>UTF-8 with a byte-order mark, which Excel, Numbers and Google Sheets all open with Thai intact.</summary>
    Csv,

    /// <summary>An Excel workbook: one sheet, typed cells, a frozen header row.</summary>
    Xlsx,

    /// <summary>A printable A4 landscape table with the clinic, the branches and the span on every page.</summary>
    Pdf,
}

/// <summary><c>queued → running → succeeded | failed</c>.</summary>
public enum ReportExportStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}
