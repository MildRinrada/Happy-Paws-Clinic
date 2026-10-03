using HappyPaws.Application.Access;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Articles;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Crm.GetMyPackageBalance;
using HappyPaws.Application.Customers;
using HappyPaws.Application.MedicalRecords;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Laboratory;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Application.Prescriptions;
using HappyPaws.Application.Queueing;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.ServiceCatalog;
using HappyPaws.Application.Staff;
using HappyPaws.Infrastructure.Assistant;
using HappyPaws.Infrastructure.Auditing;
using HappyPaws.Application.MedicalRecords.GenerateDocument;
using HappyPaws.Infrastructure.Common;
using HappyPaws.Infrastructure.Documents;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Notifications;
using HappyPaws.Infrastructure.Realtime;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.Infrastructure.Persistence.Idempotency;
using HappyPaws.Infrastructure.Persistence.Interceptors;
using HappyPaws.Infrastructure.Persistence.Repositories.Access;
using HappyPaws.Infrastructure.Persistence.Repositories.Appointments;
using HappyPaws.Infrastructure.Persistence.Repositories.Articles;
using HappyPaws.Infrastructure.Persistence.Repositories.Scheduling;
using HappyPaws.Infrastructure.Persistence.Repositories.Auditing;
using HappyPaws.Infrastructure.Persistence.Repositories.Billing;
using HappyPaws.Infrastructure.Persistence.Repositories.Clinics;
using HappyPaws.Infrastructure.Persistence.Repositories.Consultations;
using HappyPaws.Infrastructure.Persistence.Repositories.Crm;
using HappyPaws.Infrastructure.Persistence.Repositories.Customers;
using HappyPaws.Infrastructure.Persistence.Repositories.Identity;
using HappyPaws.Infrastructure.Persistence.Repositories.Laboratory;
using HappyPaws.Infrastructure.Persistence.Repositories.MedicalRecords;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;
using HappyPaws.Infrastructure.Persistence.Repositories.Patients;
using HappyPaws.Infrastructure.Persistence.Repositories.Prescriptions;
using HappyPaws.Infrastructure.Persistence.Repositories.Queueing;
using HappyPaws.Infrastructure.Persistence.Repositories.ServiceCatalog;
using HappyPaws.Infrastructure.Persistence.Repositories.Staff;
using HappyPaws.Infrastructure.Staff;
using HappyPaws.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer.
/// The API project calls this once; it never news up a <c>DbContext</c> or an external client
/// itself.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClinicOptions>()
            .Bind(configuration.GetSection(ClinicOptions.SectionName))
            .ValidateOnStart();

        // Validated at startup, not on first login: a missing or short signing key must stop the
        // process, never silently produce forgeable tokens.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddScoped<TimestampInterceptor>();

        services.AddDbContext<HappyPawsDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Postgres"),
                    npgsql => npgsql
                        .MigrationsAssembly(typeof(HappyPawsDbContext).Assembly.FullName)
                        .MigrationsHistoryTable("__migrations")
                        .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(provider.GetRequiredService<TimestampInterceptor>());
        });

        // The website assistant's model: Gemini, an OpenAI-format server (Ollama, vLLM) or none.
        services.AddAssistantModel(configuration);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        // ---- Auditing ----
        // Registered before the modules that use it: every clinical module writes through this
        // port, and it is the one dependency whose absence would be silent.
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();

        // ---- Identity module ----
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITwoFactorService, TotpService>();

        // Customer sign-in. The two senders are loud placeholders until SMS and email adapters
        // exist; see LoggingCustomerSignInSenders. The contract's OTP limit is read from config.
        services.AddScoped<ICustomerSignInRepository, CustomerSignInRepository>();
        services.AddOptions<SignInDeliveryOptions>().Bind(configuration.GetSection(SignInDeliveryOptions.SectionName));
        services.AddSingleton<IOtpSender, LoggingOtpSender>();
        services.AddSingleton<IPasswordResetSender, LoggingPasswordResetSender>();
        services.AddSingleton(new OtpSettings(configuration.GetValue("RateLimits:OtpPerPhonePer10Minutes", 3)));

        // ---- Booking and Appointment module ----
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ISlotHoldRepository, SlotHoldRepository>();
        services.AddScoped<IBookingCatalog, BookingCatalog>();
        services.AddScoped<IWaitingListRepository, WaitingListRepository>();

        // ---- Queue module ----
        services.AddScoped<IQueueTicketRepository, QueueTicketRepository>();
        services.AddScoped<IQueueNumberGenerator, QueueNumberGenerator>();

        // Singleton: the broker *is* the subscriber list, so a scoped one would hand every request
        // its own empty set of listeners and nothing would ever be delivered.
        services.AddSingleton<QueueEventBroker>();
        services.AddSingleton<IQueueEventStream>(provider => provider.GetRequiredService<QueueEventBroker>());

        // ---- Customers, consent and data-subject rights ----
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerInvitationRepository, CustomerInvitationRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<IDataSubjectRequestRepository, DataSubjectRequestRepository>();

        // ---- Notifications ----
        // The channel is a loud placeholder until LINE/SMS/email land. It logs at warning rather
        // than dropping silently; see LoggingNotificationChannel.
        services.AddSingleton<INotificationChannel, LoggingNotificationChannel>();

        // Use cases resolve the dispatcher, and the only dispatcher there is checks consent before
        // handing anything to a channel. That is what makes the portal toggles binding: a use case
        // cannot route around a check it has no way to reach. Scoped, because consent is read from
        // the database per request.
        services.AddScoped<INotificationDispatcher, ConsentAwareNotificationDispatcher>();
        services.AddScoped<IInboxRepository, InboxRepository>();

        // ---- Loyalty and packages (the read half; selling is Tier 3) ----
        services.AddScoped<ICustomerPackageRepository, CustomerPackageRepository>();

        // ---- Consultation and medical record ----
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<ITreatmentTemplateRepository, TreatmentTemplateRepository>();

        // ---- Laboratory and pharmacy ----
        services.AddScoped<ILabRequestRepository, LabRequestRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

        // ---- Patients ----
        // The real ownership query, replacing the fail-closed stand-in that stood here while the
        // pet tables did not exist.
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IPetOwnershipRepository, PetOwnershipRepository>();
        services.AddScoped<IPetTimelineReader, PetTimelineReader>();
        services.AddScoped<IPetAccessPolicy, PetAccessPolicy>();
        services.AddScoped<IPetAllergyRepository, PetAllergyRepository>();
        services.AddScoped<IVaccinationRepository, VaccinationRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
        services.AddScoped<IPetDocumentRepository, PetDocumentRepository>();
        services.AddScoped<IHospitalNumberGenerator, HospitalNumberGenerator>();

        // ---- Billing ----
        // The number generator is a concurrency mechanism, not a repository: see
        // DocumentNumberGenerator on why a tax sequence cannot be allocated in application code.
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();

        // ---- Clinic setup ----
        services.AddScoped<IClinicSettingsStore, ClinicSettingsStore>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();

        // ---- Staff and permissions ----
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

        // Another loud placeholder: invitations are logged, not emailed, until an email adapter
        // lands. See LoggingStaffInvitationSender on what it will and will not write to the log.
        services.AddOptions<StaffInvitationOptions>()
            .Bind(configuration.GetSection(StaffInvitationOptions.SectionName));
        services.AddSingleton<IStaffInvitationSender, LoggingStaffInvitationSender>();

        // ---- Service catalogue ----
        services.AddScoped<IServiceRepository, ServiceRepository>();

        // ---- Articles (contract extension) ----
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();

        // ---- Object storage ----
        // Local disk with signed URLs, standing in for S3 and its kin; see LocalFileStorage.
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // ---- Printed documents ----
        services.AddSingleton<IDocumentRenderer, QuestPdfRenderer>();

        // The notification adapters (LINE/SMS/email), the payment and lab gateways
        // and the outbound webhook dispatcher register here as each module is implemented.

        return services;
    }
}
