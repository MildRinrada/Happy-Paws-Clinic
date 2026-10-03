using HappyPaws.Application.Clinics.AddHoliday;
using HappyPaws.Application.Clinics.Bootstrap;
using HappyPaws.Application.Clinics.CreateBranch;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Clinics.GetOpeningHours;
using HappyPaws.Application.Clinics.ListBranches;
using HappyPaws.Application.Clinics.ManageBookingSettings;
using HappyPaws.Application.Clinics.ManageContacts;
using HappyPaws.Application.Clinics.SeedContacts;
using HappyPaws.Application.Clinics.ReplaceOpeningHours;
using HappyPaws.Application.Clinics.UpdateClinicProfile;
using HappyPaws.Application.Clinics.UpdateMedicalRecordSettings;
using HappyPaws.Application.Clinics.UpdateQueueSettings;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Clinics;

/// <summary>Registers clinic setup: profile, branches, hours, holidays and the three settings groups.</summary>
public static class ClinicsModule
{
    public static IServiceCollection AddClinicsModule(this IServiceCollection services)
    {
        services.AddScoped<GetClinicProfileHandler>();
        services.AddScoped<UpdateClinicProfileHandler>();

        services.AddScoped<ListBranchesHandler>();
        services.AddScoped<CreateBranchHandler>();

        services.AddScoped<GetOpeningHoursHandler>();
        services.AddScoped<ReplaceOpeningHoursHandler>();
        services.AddScoped<AddHolidayHandler>();

        // Contact entries: a contract extension, docs/contract-extensions.md § 15.
        services.AddScoped<ListContactsHandler>();
        services.AddScoped<AddContactHandler>();
        services.AddScoped<UpdateContactHandler>();
        services.AddScoped<RemoveContactHandler>();
        services.AddScoped<SeedContactsHandler>();

        services.AddScoped<GetBookingSettingsHandler>();
        services.AddScoped<UpdateBookingSettingsHandler>();
        services.AddScoped<UpdateQueueSettingsHandler>();
        services.AddScoped<UpdateMedicalRecordSettingsHandler>();

        // Not reachable over HTTP. Run from the server's own command line to create the first
        // owner — see Api/Operator/OperatorCommands.cs.
        services.AddScoped<BootstrapClinicHandler>();

        return services;
    }
}
