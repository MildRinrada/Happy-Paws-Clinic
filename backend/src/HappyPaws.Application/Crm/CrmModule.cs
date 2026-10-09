using HappyPaws.Application.Billing;
using HappyPaws.Application.Crm.CreateCoupon;
using HappyPaws.Application.Crm.CreatePackage;
using HappyPaws.Application.Crm.CreateSegment;
using HappyPaws.Application.Crm.GetLoyaltyAccount;
using HappyPaws.Application.Crm.GetMyPackageBalance;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Crm;

/// <summary>
/// Registers loyalty, packages and promotions: the four staff endpoints, the owner's package list,
/// and <see cref="InvoiceRewards"/>, which the billing handlers call through
/// <see cref="IInvoiceRewards"/>.
/// </summary>
public static class CrmModule
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceRewards, InvoiceRewards>();

        services.AddScoped<GetMyPackageBalanceHandler>();
        services.AddScoped<CreatePackageHandler>();
        services.AddScoped<CreateCouponHandler>();
        services.AddScoped<CreateSegmentHandler>();
        services.AddScoped<GetLoyaltyAccountHandler>();

        return services;
    }
}
