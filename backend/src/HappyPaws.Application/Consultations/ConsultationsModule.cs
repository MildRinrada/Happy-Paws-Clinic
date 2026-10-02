using HappyPaws.Application.Consultations.AddDiagnosis;
using HappyPaws.Application.Consultations.AddPrescription;
using HappyPaws.Application.Consultations.AddTreatment;
using HappyPaws.Application.Consultations.CloseConsultation;
using HappyPaws.Application.Consultations.ListTreatmentTemplates;
using HappyPaws.Application.Consultations.OpenConsultation;
using HappyPaws.Application.Consultations.RecordExamination;
using HappyPaws.Application.Consultations.RecordVitals;
using HappyPaws.Application.Consultations.SeedTreatmentTemplates;
using HappyPaws.Application.Consultations.UpdateConsultation;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Consultations;

/// <summary>Registers the eight consultation use cases and the treatment templates.</summary>
public static class ConsultationsModule
{
    public static IServiceCollection AddConsultationsModule(this IServiceCollection services)
    {
        services.AddScoped<ConsultationAccess>();

        services.AddScoped<OpenConsultationHandler>();
        services.AddScoped<UpdateConsultationHandler>();
        services.AddScoped<RecordVitalsHandler>();
        services.AddScoped<RecordExaminationHandler>();
        services.AddScoped<AddDiagnosisHandler>();
        services.AddScoped<AddTreatmentHandler>();
        services.AddScoped<AddPrescriptionHandler>();
        services.AddScoped<CloseConsultationHandler>();

        services.AddScoped<ListTreatmentTemplatesHandler>();
        services.AddScoped<SeedTreatmentTemplatesHandler>();

        return services;
    }
}
