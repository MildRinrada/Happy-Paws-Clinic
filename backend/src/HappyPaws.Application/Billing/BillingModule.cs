using HappyPaws.Application.Billing.ApplyDiscount;
using HappyPaws.Application.Billing.CollectPayment;
using HappyPaws.Application.Billing.CreateInvoice;
using HappyPaws.Application.Billing.GetInvoice;
using HappyPaws.Application.Billing.IssueReceipt;
using HappyPaws.Application.Billing.ListInvoices;
using HappyPaws.Application.Billing.ListMyInvoices;
using HappyPaws.Application.Billing.RefundPayment;
using HappyPaws.Application.Billing.VoidInvoice;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Billing;

/// <summary>Registers the billing use cases and the seam the consultation close bills through.</summary>
public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        services.AddScoped<InvoiceAccess>();
        services.AddScoped<ConsultationBilling>();

        services.AddScoped<CreateInvoiceHandler>();
        services.AddScoped<GetInvoiceHandler>();
        services.AddScoped<ListInvoicesHandler>();
        services.AddScoped<ApplyDiscountHandler>();
        services.AddScoped<CollectPaymentHandler>();
        services.AddScoped<RefundPaymentHandler>();
        services.AddScoped<IssueReceiptHandler>();
        services.AddScoped<VoidInvoiceHandler>();
        services.AddScoped<ListMyInvoicesHandler>();

        return services;
    }
}
