using HappyPaws.Application.Patients.CreatePet;
using HappyPaws.Application.Patients.CreateTreatmentPlan;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Application.Patients.GetPet;
using HappyPaws.Application.Patients.GetPetHistory;
using HappyPaws.Application.Patients.GetWeightHistory;
using HappyPaws.Application.Patients.ListPets;
using HappyPaws.Application.Patients.ListVaccinations;
using HappyPaws.Application.Patients.ManageCarers;
using HappyPaws.Application.Patients.MarkPetDeceased;
using HappyPaws.Application.Patients.Portal;
using HappyPaws.Application.Patients.RecordAllergy;
using HappyPaws.Application.Patients.RecordVaccination;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Patients;

/// <summary>Registers the patient registry, its carer links, and the Tier-2 clinical additions.</summary>
public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services)
    {
        services.AddScoped<ListPetsHandler>();
        services.AddScoped<CreatePetHandler>();
        services.AddScoped<GetPetHandler>();
        services.AddScoped<GetPetHistoryHandler>();
        services.AddScoped<AddCarerHandler>();
        services.AddScoped<RemoveCarerHandler>();

        // Built earlier, with the consultation module: prescriptions have to check allergies
        // against something, and a patient can be deceased with nothing able to say so.
        services.AddScoped<RecordAllergyHandler>();
        services.AddScoped<MarkPetDeceasedHandler>();

        services.AddScoped<ListVaccinationsHandler>();
        services.AddScoped<RecordVaccinationHandler>();
        services.AddScoped<GetWeightHistoryHandler>();
        services.AddScoped<CreateTreatmentPlanHandler>();

        services.AddScoped<DocumentUploads>();
        services.AddScoped<CreateDocumentUploadUrlHandler>();
        services.AddScoped<UploadOwnerDocumentHandler>();
        services.AddScoped<StoredFilesHandler>();

        // The owner's side: their pets, their edits, and the portal home.
        services.AddScoped<MyPetReader>();
        services.AddScoped<ListMyPetsHandler>();
        services.AddScoped<UpdatePetByOwnerHandler>();
        services.AddScoped<GetPortalHomeHandler>();

        return services;
    }
}
