using System.Reflection;
using FluentValidation;
using HappyPaws.Application.Access;
using HappyPaws.Application.Articles;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Assistant;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Laboratory;
using HappyPaws.Application.MedicalRecords;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Prescriptions;
using HappyPaws.Application.Queueing;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.ServiceCatalog;
using HappyPaws.Application.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application;

/// <summary>
/// Composition root for the Application layer.
/// <para>
/// Registers the cross-cutting services every module leans on. Module use cases register
/// themselves from their own folders as they are implemented — this file does not become a list of
/// every handler in the system.
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Validators are discovered by convention: a validator placed next to its use case is
        // picked up without an entry here.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.AddScoped<RequestValidator>();

        // Resource-level authorization guards. BranchGuard decides from the token alone;
        // PetAccessGuard needs IPetAccessPolicy, which Infrastructure supplies once the
        // Patients module lands.
        services.AddScoped<BranchGuard>();
        services.AddScoped<PetAccessGuard>();

        services.AddIdentityModule();
        services.AddAppointmentsModule();
        services.AddQueueingModule();
        services.AddAuditingModule();
        services.AddConsultationsModule();
        services.AddMedicalRecordsModule();
        services.AddPatientsModule();
        services.AddCustomersModule();
        services.AddBillingModule();
        services.AddClinicsModule();
        services.AddAccessModule();
        services.AddStaffModule();
        services.AddServiceCatalogModule();
        services.AddArticlesModule();
        services.AddSchedulingModule();
        services.AddLaboratoryModule();
        services.AddPrescriptionsModule();
        services.AddNotificationsModule();
        services.AddCrmModule();
        services.AddAssistantModule();

        return services;
    }
}
