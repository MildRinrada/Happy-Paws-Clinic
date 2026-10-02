using HappyPaws.Application.MedicalRecords.AmendMedicalRecord;
using HappyPaws.Application.MedicalRecords.GenerateDocument;
using HappyPaws.Application.MedicalRecords.GetMedicalRecord;
using HappyPaws.Application.MedicalRecords.GetOwnerVisibleRecords;
using HappyPaws.Application.MedicalRecords.GetRecordAudit;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.MedicalRecords;

/// <summary>Registers the signed-record use cases: read, amend, review the history, and the owner's view.</summary>
public static class MedicalRecordsModule
{
    public static IServiceCollection AddMedicalRecordsModule(this IServiceCollection services)
    {
        services.AddScoped<GetMedicalRecordHandler>();
        services.AddScoped<AmendMedicalRecordHandler>();
        services.AddScoped<GetRecordAuditHandler>();
        services.AddScoped<GetOwnerVisibleRecordsHandler>();
        services.AddScoped<GenerateDocumentHandler>();

        return services;
    }
}
