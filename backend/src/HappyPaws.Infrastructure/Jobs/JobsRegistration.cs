using HappyPaws.Application.Automation;
using HappyPaws.Application.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// Registers the scheduler: the jobs themselves (always, so tests can run them by hand) and one
/// <see cref="RecurringJobHost{TJob}"/> per job, which decides at start-up from <c>Jobs:*</c>
/// whether to loop at all.
/// </summary>
public static class JobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JobsOptions>().Bind(configuration.GetSection(JobsOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var automations = provider.GetRequiredService<IOptions<JobsOptions>>().Value.Automations;

            return new AutomationSettings(
                TimeSpan.FromHours(Math.Clamp(automations.CatchUpHours, 1, 72)),
                $"{Environment.MachineName}:{Environment.ProcessId}");
        });

        services.AddSingleton<JobSignal>();
        services.AddSingleton<IReportExportSignal>(provider => provider.GetRequiredService<JobSignal>());

        services.AddScoped<AutomationJob>();
        services.AddScoped<ReportExportJob>();
        services.AddScoped<HousekeepingJob>();

        AddHost<AutomationJob>(services, "automations", options => options.Automations);
        AddHost<ReportExportJob>(services, JobSignal.ReportExports, options => options.ReportExports);
        AddHost<HousekeepingJob>(services, "housekeeping", options => options.Housekeeping);

        return services;
    }

    private static void AddHost<TJob>(IServiceCollection services, string name, Func<JobsOptions, JobSchedule> schedule)
        where TJob : IScheduledJob =>
        services.AddHostedService(provider => new RecurringJobHost<TJob>(
            name,
            schedule,
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<JobsOptions>>(),
            provider.GetRequiredService<JobSignal>(),
            provider.GetRequiredService<ILogger<RecurringJobHost<TJob>>>()));
}
