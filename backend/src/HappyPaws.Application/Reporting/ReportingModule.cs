using HappyPaws.Application.Reporting.Csv;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.GetExportStatus;
using HappyPaws.Application.Reporting.RunExports;
using HappyPaws.Application.Reporting.StartExport;
using HappyPaws.Application.Reporting.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Reporting;

/// <summary>Registers report exports: the two endpoints, the worker that builds the files, and the CSV writer.</summary>
public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<ReportTableBuilder>();

        // CSV needs nothing outside this layer; the XLSX and PDF writers come with Infrastructure.
        services.AddSingleton<IReportFileWriter, CsvReportWriter>();
        services.AddScoped<StartExportHandler>();
        services.AddScoped<GetExportStatusHandler>();
        services.AddScoped<RunReportExportsHandler>();

        return services;
    }
}
