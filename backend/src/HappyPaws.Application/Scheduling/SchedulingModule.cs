using HappyPaws.Application.Scheduling.CreateShifts;
using HappyPaws.Application.Scheduling.GetScheduleCalendar;
using HappyPaws.Application.Scheduling.GetStaffAvailability;
using HappyPaws.Application.Scheduling.ReplaceAssignmentRules;
using HappyPaws.Application.Scheduling.RequestTimeOff;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Scheduling;

/// <summary>Registers the roster: shifts, time off, the calendar, free time and assignment rules.</summary>
public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddScoped<ClinicDays>();
        services.AddScoped<ScheduleAccess>();
        services.AddScoped<SubstituteFinder>();

        services.AddScoped<CreateShiftsHandler>();
        services.AddScoped<RequestTimeOffHandler>();
        services.AddScoped<GetScheduleCalendarHandler>();
        services.AddScoped<GetStaffAvailabilityHandler>();
        services.AddScoped<ReplaceAssignmentRulesHandler>();

        return services;
    }
}
