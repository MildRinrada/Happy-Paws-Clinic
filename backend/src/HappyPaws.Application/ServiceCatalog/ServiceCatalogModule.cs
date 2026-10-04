using HappyPaws.Application.ServiceCatalog.CreateService;
using HappyPaws.Application.ServiceCatalog.ListBookableServices;
using HappyPaws.Application.ServiceCatalog.ListServices;
using HappyPaws.Application.ServiceCatalog.UpdateAvailability;
using HappyPaws.Application.ServiceCatalog.UpdatePricing;
using HappyPaws.Application.ServiceCatalog.UpdateService;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.ServiceCatalog;

/// <summary>
/// Registers the service catalogue: the public menu, the three <c>service:manage</c> writes, and
/// the staff list and edit from <c>docs/contract-extensions.md § 17</c>.
/// </summary>
public static class ServiceCatalogModule
{
    public static IServiceCollection AddServiceCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ListBookableServicesHandler>();
        services.AddSingleton<ListServiceCategoriesHandler>();
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdatePricingHandler>();
        services.AddScoped<UpdateAvailabilityHandler>();
        services.AddScoped<ListServicesHandler>();
        services.AddScoped<UpdateServiceHandler>();

        return services;
    }
}
