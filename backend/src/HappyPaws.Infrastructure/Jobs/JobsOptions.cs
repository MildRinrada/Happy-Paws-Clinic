namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// <c>Jobs:*</c> — the background scheduler.
/// <code>
/// "Jobs": {
///   "Enabled": true,
///   "StartupDelaySeconds": 10,
///   "Automations":   { "Enabled": true, "IntervalSeconds": 60, "CatchUpHours": 12 },
///   "ReportExports": { "Enabled": true, "IntervalSeconds": 15 },
///   "Housekeeping":  { "Enabled": true, "IntervalSeconds": 3600 },
///   "Webhooks":      { "Enabled": true, "IntervalSeconds": 10 }
/// }
/// </code>
/// <para>
/// <see cref="Enabled"/> false switches every job off on this instance. The integration test host
/// sets it false, and tests drive the jobs directly. Several instances may run with it on: every
/// job claims its work in the database (see <c>AutomationDispatch</c> and <c>ReportExport</c>), so
/// two schedulers never do the same piece twice.
/// </para>
/// </summary>
public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public bool Enabled { get; set; } = true;

    /// <summary>A pause before the first run, so a starting instance serves requests first.</summary>
    public int StartupDelaySeconds { get; set; } = 10;

    public AutomationJobOptions Automations { get; set; } = new();

    public JobSchedule ReportExports { get; set; } = new() { IntervalSeconds = 15 };

    public JobSchedule Housekeeping { get; set; } = new() { IntervalSeconds = 3_600 };

    /// <summary>The webhook outbox and its deliveries. Each claims its rows, so instances share the work.</summary>
    public JobSchedule Webhooks { get; set; } = new() { IntervalSeconds = 10 };
}

/// <summary>One job's switch and how often it runs.</summary>
public class JobSchedule
{
    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 60;

    /// <summary>At least a second, whatever was configured.</summary>
    public TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(1, IntervalSeconds));
}

/// <summary>The reminder job, with how late a missed reminder may still go out.</summary>
public sealed class AutomationJobOptions : JobSchedule
{
    public int CatchUpHours { get; set; } = 12;
}
