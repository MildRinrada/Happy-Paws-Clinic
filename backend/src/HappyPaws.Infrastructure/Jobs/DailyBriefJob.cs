using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.StaffAi.DailyBrief;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// Each tick: write the day's morning brief for every branch that has none yet, once the clinic's
/// clock has passed <c>StaffAi:DailyBriefHour</c> (<c>docs/contract-extensions.md § 34</c>). Does
/// nothing while the staff AI is off. Each brief is claimed in the database first, so several
/// instances never write the same branch's brief twice.
/// </summary>
public sealed class DailyBriefJob(GenerateDailyBriefsHandler handler, IClock clock, ILogger<DailyBriefJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var written = await handler.RunAsync(clock.UtcNow, cancellationToken);

        if (written > 0)
        {
            logger.LogInformation("Daily briefs: {Count} written", written);
        }
    }
}
