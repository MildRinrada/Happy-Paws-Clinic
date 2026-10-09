using HappyPaws.Application.Appointments.AddToWaitingList;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.BookAppointment;
using HappyPaws.Application.Appointments.CancelAppointment;
using HappyPaws.Application.Appointments.CheckInAppointment;
using HappyPaws.Application.Appointments.ConfirmAppointment;
using HappyPaws.Application.Appointments.CreateRecurringSeries;
using HappyPaws.Application.Appointments.CreateStaffAppointment;
using HappyPaws.Application.Appointments.DetectConflicts;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Appointments.GetAppointmentRules;
using HappyPaws.Application.Appointments.GetAvailability;
using HappyPaws.Application.Appointments.HoldSlot;
using HappyPaws.Application.Appointments.ListAppointments;
using HappyPaws.Application.Appointments.ListMyAppointments;
using HappyPaws.Application.Appointments.ListWaitingList;
using HappyPaws.Application.Appointments.MarkNoShow;
using HappyPaws.Application.Appointments.RemoveFromWaitingList;
using HappyPaws.Application.Appointments.ReplaceAppointmentTypes;
using HappyPaws.Application.Appointments.ReplaceCancellationPolicy;
using HappyPaws.Application.Appointments.RescheduleAppointment;
using HappyPaws.Application.Appointments.UpdateAppointment;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// Registers the Booking and Appointment use cases — the twelve endpoints of that section of the
/// contract and the six of Advanced appointment, plus the services they share.
/// </summary>
public static class AppointmentsModule
{
    public static IServiceCollection AddAppointmentsModule(this IServiceCollection services)
    {
        services.AddScoped<BookingSlotResolver>();
        services.AddScoped<AppointmentAccess>();
        services.AddScoped<SeriesBooker>();
        services.AddScoped<WaitingListNotifier>();

        services.AddScoped<GetAvailabilityHandler>();
        services.AddScoped<HoldSlotHandler>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<CreateStaffAppointmentHandler>();
        services.AddScoped<ListAppointmentsHandler>();
        services.AddScoped<ListMyAppointmentsHandler>();
        services.AddScoped<GetAppointmentHandler>();
        services.AddScoped<ConfirmAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddScoped<CheckInAppointmentHandler>();
        services.AddScoped<UpdateAppointmentHandler>();

        services.AddScoped<MarkNoShowHandler>();
        services.AddScoped<CreateRecurringSeriesHandler>();
        services.AddScoped<DetectConflictsHandler>();
        services.AddScoped<AddToWaitingListHandler>();
        services.AddScoped<ReplaceAppointmentTypesHandler>();
        services.AddScoped<ReplaceCancellationPolicyHandler>();

        // docs/contract-extensions.md § 23: reading the two rule sets back, and listing and
        // withdrawing waiting-list entries.
        services.AddScoped<GetCancellationPolicyHandler>();
        services.AddScoped<GetAppointmentTypesHandler>();
        services.AddScoped<ListOnlineAppointmentTypesHandler>();
        services.AddScoped<ListWaitingListHandler>();
        services.AddScoped<RemoveFromWaitingListHandler>();

        return services;
    }
}
