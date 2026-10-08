namespace HappyPaws.Application.Automation;

/// <summary>
/// How far back a run looks for reminders it has not sent yet, and who this worker is.
/// <para>
/// The catch-up window is what makes a missed tick harmless. A reminder due at 09:00 that a
/// restarting API did not evaluate until 09:04 is still sent at 09:04; one due at 09:00 on a day
/// the scheduler was off until 23:00 is not, because a "tomorrow at 10:30" message that arrives
/// fourteen hours late reads as wrong. The simulation and the upcoming list use the same window,
/// which is part of why they cannot disagree with the real run.
/// </para>
/// </summary>
/// <param name="CatchUp">How long after its send time a reminder may still go out.</param>
/// <param name="WorkerName">This process, written on every claim it makes.</param>
public sealed record AutomationSettings(TimeSpan CatchUp, string WorkerName)
{
    public static readonly TimeSpan DefaultCatchUp = TimeSpan.FromHours(12);

    /// <summary>A claim older than this with no delivery recorded belonged to a worker that stopped.</summary>
    public static readonly TimeSpan StaleClaim = TimeSpan.FromMinutes(10);
}
