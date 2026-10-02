using HappyPaws.Application.Staff.AcceptInvitation;
using HappyPaws.Application.Staff.DeactivateStaffMember;
using HappyPaws.Application.Staff.InviteStaffMember;
using HappyPaws.Application.Staff.ListDepartments;
using HappyPaws.Application.Staff.ListStaffMembers;
using HappyPaws.Application.Staff.ListStaffTypes;
using HappyPaws.Application.Staff.ListTeam;
using HappyPaws.Application.Staff.ReplacePublicProfile;
using HappyPaws.Application.Staff.SeedStaff;
using HappyPaws.Application.Staff.UpdateStaffMember;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Staff;

/// <summary>Registers staff management: the list, invitations, and changing or ending a membership.</summary>
public static class StaffModule
{
    public static IServiceCollection AddStaffModule(this IServiceCollection services)
    {
        services.AddScoped<StaffAssignmentResolver>();
        services.AddScoped<StaffMemberAccess>();

        services.AddScoped<ListStaffMembersHandler>();
        services.AddScoped<ListStaffTypesHandler>();
        services.AddScoped<InviteStaffMemberHandler>();
        services.AddScoped<AcceptInvitationHandler>();
        services.AddScoped<UpdateStaffMemberHandler>();
        services.AddScoped<DeactivateStaffMemberHandler>();

        // Departments, public profiles and the public team page (extension, § 10).
        services.AddScoped<ListDepartmentsHandler>();
        services.AddScoped<ReplacePublicProfileHandler>();
        services.AddScoped<ListTeamHandler>();
        services.AddScoped<SeedStaffHandler>();

        return services;
    }
}
