using HappyPaws.Application.Crm.GetMyPackageBalance;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Crm;

/// <summary>Registers the loyalty module's one built endpoint: the customer's package balance.</summary>
public static class CrmModule
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services)
    {
        services.AddScoped<GetMyPackageBalanceHandler>();

        return services;
    }
}
