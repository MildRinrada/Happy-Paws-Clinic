using HappyPaws.Application.Access.CreateRole;
using HappyPaws.Application.Access.ListRoles;
using HappyPaws.Application.Access.ReplaceRolePermissions;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Access;

/// <summary>Registers role management and the two rules every change to access passes through.</summary>
public static class AccessModule
{
    public static IServiceCollection AddAccessModule(this IServiceCollection services)
    {
        services.AddScoped<ScopeGrantPolicy>();
        services.AddScoped<OwnerLockoutGuard>();

        services.AddScoped<ListRolesHandler>();
        services.AddSingleton<ListPermissionCatalogueHandler>();
        services.AddScoped<CreateRoleHandler>();
        services.AddScoped<ReplaceRolePermissionsHandler>();

        return services;
    }
}
