using HappyPaws.Application.Laboratory.CreateLabRequest;
using HappyPaws.Application.Laboratory.ListLabRequests;
using HappyPaws.Application.Laboratory.RecordLabResults;
using HappyPaws.Application.Laboratory.ReviewLabResults;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Laboratory;

/// <summary>Registers lab requests, their results and the worklist.</summary>
public static class LaboratoryModule
{
    public static IServiceCollection AddLaboratoryModule(this IServiceCollection services)
    {
        services.AddScoped<LabRequestAccess>();
        services.AddScoped<CreateLabRequestHandler>();
        services.AddScoped<RecordLabResultsHandler>();
        services.AddScoped<ListLabRequestsHandler>();
        services.AddScoped<ReviewLabResultsHandler>();

        return services;
    }
}
