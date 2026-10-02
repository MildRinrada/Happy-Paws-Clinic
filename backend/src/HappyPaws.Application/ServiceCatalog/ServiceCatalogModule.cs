using HappyPaws.Application.ServiceCatalog.CreateService;
using HappyPaws.Application.ServiceCatalog.ListBookableServices;
using HappyPaws.Application.ServiceCatalog.UpdateAvailability;
using HappyPaws.Application.ServiceCatalog.UpdatePricing;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.ServiceCatalog;

/// <summary>Registers the service catalogue: the public menu and the three <c>service:manage</c> writes.</summary>
public static class ServiceCatalogModule
{
    public static IServiceCollection AddServiceCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ListBookableServicesHandler>();
        services.AddSingleton<ListServiceCategoriesHandler>();
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdatePricingHandler>();
        services.AddScoped<UpdateAvailabilityHandler>();

        return services;
    }
}
