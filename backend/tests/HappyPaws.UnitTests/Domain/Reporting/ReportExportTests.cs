using HappyPaws.Domain.Common;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.UnitTests.Domain.Reporting;

/// <summary>An export job's life: queued, running, and how it ends or is retried.</summary>
public class ReportExportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 3, 0, 0, TimeSpan.Zero);

    private static ReportExport Queued(IReadOnlyCollection<string>? branches = null) => ReportExport.Request(
        ReportKind.Revenue, ReportFormat.Csv, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, branches, "stf_1");

    [Fact]
    public void The_branches_are_fixed_when_it_is_asked_for()
    {
        var everywhere = Queued();
        var mine = Queued(["brn_b", "brn_a", "brn_a"]);

        Assert.True(everywhere.AllBranches);
        Assert.Empty(everywhere.BranchIds);
        Assert.False(mine.AllBranches);
        Assert.Equal(["brn_a", "brn_b"], mine.BranchIds);
        Assert.StartsWith("rex_", mine.Id);
        Assert.Equal(ReportExportStatus.Queued, mine.Status);
    }

    [Fact]
    public void A_span_that_ends_before_it_starts_is_refused()
    {
        Assert.Throws<DomainException>(() => ReportExport.Request(
            ReportKind.Queue, ReportFormat.Csv, new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1), null, null, "stf_1"));
    }

    [Fact]
    public void A_job_runs_then_succeeds_with_its_file()
    {
        var export = Queued();

        export.Start(Now);
        Assert.Equal(ReportExportStatus.Running, export.Status);
        Assert.Equal(1, export.Attempts);

        export.Succeed("exports/rex_1.csv", "revenue.csv", 120, 3, Now.AddSeconds(2));
        Assert.Equal(ReportExportStatus.Succeeded, export.Status);
        Assert.Equal(3, export.RowCount);
        Assert.Throws<StateConflictException>(() => export.Start(Now));
    }

    [Fact]
    public void A_job_cannot_finish_without_having_started()
    {
        var export = Queued();

        Assert.Throws<StateConflictException>(() => export.Fail("export_failed", Now));
        Assert.Throws<StateConflictException>(() => export.Succeed("k", "f", 1, 1, Now));
    }

    [Fact]
    public void An_abandoned_job_is_retried_until_it_has_used_its_attempts()
    {
        var export = Queued();

        for (var attempt = 1; attempt < ReportExport.MaxAttempts; attempt++)
        {
            export.Start(Now);
            export.Abandon(Now);
            Assert.Equal(ReportExportStatus.Queued, export.Status);
        }

        export.Start(Now);
        export.Abandon(Now);

        Assert.Equal(ReportExportStatus.Failed, export.Status);
        Assert.Equal("export_timed_out", export.ErrorCode);
    }
}
