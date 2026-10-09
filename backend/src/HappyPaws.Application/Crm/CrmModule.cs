using HappyPaws.Application.Billing;
using HappyPaws.Application.Crm.AdjustPoints;
using HappyPaws.Application.Crm.CreateCoupon;
using HappyPaws.Application.Crm.CreatePackage;
using HappyPaws.Application.Crm.CreateSegment;
using HappyPaws.Application.Crm.DeactivateCoupon;
using HappyPaws.Application.Crm.GetLoyaltyAccount;
using HappyPaws.Application.Crm.GetMyPackageBalance;
using HappyPaws.Application.Crm.GetSegment;
using HappyPaws.Application.Crm.ListCoupons;
using HappyPaws.Application.Crm.ListPackages;
using HappyPaws.Application.Crm.ListSegments;
using HappyPaws.Application.Crm.ManageLoyaltySettings;
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

        // docs/contract-extensions.md § 25: managing what the endpoints above created.
        services.AddScoped<ListCouponsHandler>();
        services.AddScoped<DeactivateCouponHandler>();
        services.AddScoped<ListSegmentsHandler>();
        services.AddScoped<GetSegmentHandler>();
        services.AddScoped<ListPackagesHandler>();
        services.AddScoped<AdjustPointsHandler>();
        services.AddScoped<GetLoyaltySettingsHandler>();
        services.AddScoped<ReplaceLoyaltySettingsHandler>();

        return services;
    }
}
