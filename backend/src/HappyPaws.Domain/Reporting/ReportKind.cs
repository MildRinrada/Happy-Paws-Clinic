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
/// The file an export produces. CSV only: UTF-8 with a byte-order mark, which Excel, Numbers and
/// Google Sheets all open with Thai intact, and no spreadsheet library to carry.
/// </summary>
public enum ReportFormat
{
    Csv,
}

/// <summary><c>queued → running → succeeded | failed</c>.</summary>
public enum ReportExportStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}
