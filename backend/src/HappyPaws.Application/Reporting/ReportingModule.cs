using HappyPaws.Application.Reporting.GetExportStatus;
using HappyPaws.Application.Reporting.RunExports;
using HappyPaws.Application.Reporting.StartExport;
using HappyPaws.Application.Reporting.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Reporting;

/// <summary>Registers report exports: the two endpoints and the worker that builds the files.</summary>
public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<ReportTableBuilder>();
        services.AddScoped<StartExportHandler>();
        services.AddScoped<GetExportStatusHandler>();
        services.AddScoped<RunReportExportsHandler>();

        return services;
    }
}
