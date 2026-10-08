using HappyPaws.Application.Prescriptions.DispensePrescription;
using HappyPaws.Application.Prescriptions.ListDrugOptions;
using HappyPaws.Application.Prescriptions.ListPrescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Prescriptions;

/// <summary>
/// Registers the pharmacy side of prescriptions: the medication history, dispensing, and the
/// prescriber's drug picker (<c>docs/contract-extensions.md § 20</c>). Writing a prescription stays
/// with the consultation, which is where a vet writes one.
/// </summary>
public static class PrescriptionsModule
{
    public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
    {
        services.AddScoped<ListPrescriptionsHandler>();
        services.AddScoped<DispensePrescriptionHandler>();
        services.AddScoped<ListDrugOptionsHandler>();

        return services;
    }
}
