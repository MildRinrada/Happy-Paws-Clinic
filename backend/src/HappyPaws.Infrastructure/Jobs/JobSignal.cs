using System.Collections.Concurrent;
using HappyPaws.Application.Reporting;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// Wakes a job early, in this process. A queued export is picked up within moments instead of at
/// the next poll. Only a hint: a job always runs on its interval too, and an instance with the
/// scheduler off ignores it, so another instance's worker still finds the job in the table.
/// </summary>
public sealed class JobSignal : IReportExportSignal
{
    public const string ReportExports = "report-exports";

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    public void ExportQueued() => Wake(ReportExports);

    public void Wake(string job)
    {
        var gate = Gate(job);

        // At most one pending wake: ten requests in a burst need one run, not ten.
        if (gate.CurrentCount == 0)
        {
            try
            {
                gate.Release();
            }
            catch (SemaphoreFullException)
            {
                // Another caller woke it in between; one wake is enough.
            }
        }
    }

    /// <summary>Waits for the interval to pass or for a wake, whichever is first.</summary>
    public async Task WaitAsync(string job, TimeSpan interval, CancellationToken cancellationToken)
    {
        await Gate(job).WaitAsync(interval, cancellationToken);
    }

    private SemaphoreSlim Gate(string job) => _gates.GetOrAdd(job, _ => new SemaphoreSlim(0, 1));
}
