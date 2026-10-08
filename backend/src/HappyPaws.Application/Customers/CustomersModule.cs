using HappyPaws.Application.Customers.CreateCustomer;
using HappyPaws.Application.Customers.DataRequests;
using HappyPaws.Application.Customers.GetConsents;
using HappyPaws.Application.Customers.GetCustomer;
using HappyPaws.Application.Customers.InviteToPortal;
using HappyPaws.Application.Customers.LineAddress;
using HappyPaws.Application.Customers.MergeCustomers;
using HappyPaws.Application.Customers.RecordConsent;
using HappyPaws.Application.Customers.SearchCustomers;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Customers;

/// <summary>Registers the customer register, the portal invitation, the merge, and consent.</summary>
public static class CustomersModule
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddScoped<ConsentGate>();

        services.AddScoped<SearchCustomersHandler>();
        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<GetCustomerHandler>();
        services.AddScoped<InviteToPortalHandler>();
        services.AddScoped<MergeCustomersHandler>();

        // The LINE address the counter picks from recent followers (docs/contract-extensions.md § 18).
        services.AddScoped<LinkLineAddressHandler>();
        services.AddScoped<UnlinkLineAddressHandler>();

        // Built first, ahead of the rest of the module, because these carry a legal deadline.
        services.AddScoped<GetConsentsHandler>();
        services.AddScoped<RecordConsentHandler>();
        services.AddScoped<RaiseDataRequestHandler>();
        services.AddScoped<ListDataRequestsHandler>();
        services.AddScoped<CloseDataRequestHandler>();

        return services;
    }
}
