using HappyPaws.Application.Automation.CreateAutomation;
using HappyPaws.Application.Automation.DeleteAutomation;
using HappyPaws.Application.Automation.ListAutomations;
using HappyPaws.Application.Automation.ListUpcomingReminders;
using HappyPaws.Application.Automation.RunAutomations;
using HappyPaws.Application.Automation.SimulateAutomation;
using HappyPaws.Application.Automation.UpdateAutomation;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Automation;

/// <summary>
/// Registers the reminder rules: the evaluator every path shares, the scheduler's run, and the
/// four endpoints of the contract, plus the edit and delete of <c>docs/contract-extensions.md § 19</c>. <see cref="AutomationSettings"/> comes from Infrastructure, which reads
/// <c>Jobs:Automations</c>.
/// </summary>
public static class AutomationModule
{
    public static IServiceCollection AddAutomationModule(this IServiceCollection services)
    {
        services.AddScoped<AutomationEvaluator>();
        services.AddScoped<AutomationAccess>();

        services.AddScoped<RunAutomationsHandler>();
        services.AddScoped<ListAutomationsHandler>();
        services.AddScoped<CreateAutomationHandler>();
        services.AddScoped<UpdateAutomationHandler>();
        services.AddScoped<DeleteAutomationHandler>();
        services.AddScoped<SimulateAutomationHandler>();
        services.AddScoped<ListUpcomingRemindersHandler>();

        return services;
    }
}
