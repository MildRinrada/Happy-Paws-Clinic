using HappyPaws.Application.Forecasting.Attendance;
using HappyPaws.Application.Forecasting.Demand;
using HappyPaws.Application.Forecasting.DueBack;
using HappyPaws.Application.Forecasting.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Forecasting;

/// <summary>
/// Registers the statistical forecasts of <c>docs/contract-extensions.md § 33</c>: four reads worked
/// out from the clinic's own records with plain arithmetic, on this server. No model, no outside
/// service, nothing stored.
/// </summary>
public static class ForecastingModule
{
    public static IServiceCollection AddForecastingModule(this IServiceCollection services)
    {
        services.AddScoped<ForecastBranches>();

        services.AddScoped<GetInventoryForecastHandler>();
        services.AddScoped<GetDemandForecastHandler>();
        services.AddScoped<GetAttendanceRiskHandler>();
        services.AddScoped<ListDueBackHandler>();

        return services;
    }
}
