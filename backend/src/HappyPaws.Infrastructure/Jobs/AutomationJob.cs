using HappyPaws.Application.Automation.RunAutomations;
using HappyPaws.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>Each tick: run every enabled reminder rule at the current instant.</summary>
public sealed class AutomationJob(RunAutomationsHandler handler, IClock clock, ILogger<AutomationJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var summary = await handler.RunAsync(clock.UtcNow, cancellationToken);

        if (summary.Claimed > 0 || summary.Contended > 0 || summary.Reconciled > 0)
        {
            logger.LogInformation(
                "Automations: {Rules} rule(s), {Claimed} reminder(s) sent, {Contended} taken by another instance, {Reconciled} stale claim(s) settled",
                summary.Rules,
                summary.Claimed,
                summary.Contended,
                summary.Reconciled);
        }
    }
}
