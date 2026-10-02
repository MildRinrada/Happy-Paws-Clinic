using HappyPaws.Application.Auditing.ListAuditLogs;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Auditing;

/// <summary>
/// Registers the audit read endpoints. Writing is not a use case — every module calls
/// <see cref="IAuditTrail"/> directly, which is registered by Infrastructure.
/// </summary>
public static class AuditingModule
{
    public static IServiceCollection AddAuditingModule(this IServiceCollection services)
    {
        services.AddScoped<ListAuditLogsHandler>();

        return services;
    }
}
