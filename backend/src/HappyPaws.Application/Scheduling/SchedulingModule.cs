using HappyPaws.Application.Scheduling.ChangeShift;
using HappyPaws.Application.Scheduling.CopyWeek;
using HappyPaws.Application.Scheduling.CreateShifts;
using HappyPaws.Application.Scheduling.GetAssignmentRules;
using HappyPaws.Application.Scheduling.GetScheduleCalendar;
using HappyPaws.Application.Scheduling.GetStaffAvailability;
using HappyPaws.Application.Scheduling.RemoveShift;
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

        // docs/contract-extensions.md § 47 to 49: change and remove a shift, read the rules, copy a week.
        services.AddScoped<ChangeShiftHandler>();
        services.AddScoped<RemoveShiftHandler>();
        services.AddScoped<GetAssignmentRulesHandler>();
        services.AddScoped<CopyWeekHandler>();

        return services;
    }
}
