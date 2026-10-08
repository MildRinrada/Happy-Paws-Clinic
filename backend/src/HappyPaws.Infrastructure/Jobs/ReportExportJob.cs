using HappyPaws.Application.Reporting.RunExports;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>Each tick, or when woken by a new request: build the queued export files.</summary>
public sealed class ReportExportJob(RunReportExportsHandler handler) : IScheduledJob
{
    public Task RunAsync(CancellationToken cancellationToken) => handler.RunAsync(cancellationToken);
}
