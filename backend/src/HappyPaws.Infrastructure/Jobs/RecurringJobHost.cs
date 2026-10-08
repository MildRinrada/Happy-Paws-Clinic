using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// Runs one <see cref="IScheduledJob"/> on an interval, for as long as the host runs.
/// <para>
/// A plain <see cref="BackgroundService"/> rather than a scheduling library. The jobs here run
/// every few seconds to an hour and claim their own work in the database, so what a library would
/// add (cron expressions, a job store, cluster locks) is either unneeded or already done by the
/// rows themselves.
/// </para>
/// <para>
/// Whether to run is read when the host starts, from <see cref="JobsOptions"/>: the global switch
/// and the job's own. A run that throws is logged and the loop goes on; one bad tick must not stop
/// reminders for the rest of the day.
/// </para>
/// </summary>
/// <typeparam name="TJob">The job, registered as scoped.</typeparam>
public sealed class RecurringJobHost<TJob>(
    string name,
    Func<JobsOptions, JobSchedule> schedule,
    IServiceScopeFactory scopes,
    IOptions<JobsOptions> options,
    JobSignal signal,
    ILogger<RecurringJobHost<TJob>> logger) : BackgroundService
    where TJob : IScheduledJob
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var own = schedule(settings);

        if (!settings.Enabled || !own.Enabled)
        {
            logger.LogInformation("Background job {Job} is switched off", name);
            return;
        }

        logger.LogInformation("Background job {Job} runs every {Interval}", name, own.Interval);

        await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, settings.StartupDelaySeconds)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await signal.WaitAsync(name, own.Interval, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TJob>().RunAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Background job {Job} failed; it will run again at its next tick", name);
        }
    }
}
