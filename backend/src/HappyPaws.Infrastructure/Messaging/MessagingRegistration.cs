using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Messaging.Sms;
using HappyPaws.Infrastructure.Staff;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Messaging;

/// <summary>
/// Wires the sign-in and invitation senders to email and SMS when those are configured, and to the
/// log placeholders when they are not.
/// <para>
/// Decided when a sender is first resolved, from <see cref="IOptions{TOptions}"/>, not while the
/// container is being built. Configuration a host adds after <c>AddInfrastructure</c> (a test
/// fixture, an environment variable read late) is therefore seen, the mistake the assistant's
/// registration made once.
/// </para>
/// </summary>
internal static class MessagingRegistration
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<SmsOptions>().Bind(configuration.GetSection(SmsOptions.SectionName));
        services.AddOptions<SignInDeliveryOptions>().Bind(configuration.GetSection(SignInDeliveryOptions.SectionName));
        services.AddOptions<StaffInvitationOptions>().Bind(configuration.GetSection(StaffInvitationOptions.SectionName));

        services.AddHttpClient(ThaiBulkSmsTransport.ClientName, client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<ClinicNameReader>();
        services.AddSingleton<SmtpEmailTransport>();
        services.AddSingleton<ThaiBulkSmsTransport>();
        services.AddSingleton<LoggingOtpSender>();
        services.AddSingleton<LoggingPasswordResetSender>();
        services.AddSingleton<LoggingStaffInvitationSender>();

        services.AddSingleton<IOtpSender>(provider =>
            Sms(provider) is { } sms
                ? new SmsOtpSender(sms, provider.GetRequiredService<ClinicNameReader>(), provider.GetRequiredService<ILogger<SmsOtpSender>>())
                : provider.GetRequiredService<LoggingOtpSender>());

        services.AddSingleton<IPasswordResetSender>(provider => new DeliveringPasswordResetSender(
            Email(provider),
            Sms(provider),
            provider.GetRequiredService<LoggingPasswordResetSender>(),
            provider.GetRequiredService<ClinicNameReader>(),
            provider.GetRequiredService<IOptions<SignInDeliveryOptions>>(),
            provider.GetRequiredService<ILogger<DeliveringPasswordResetSender>>()));

        services.AddSingleton<IStaffInvitationSender>(provider =>
            Email(provider) is { } email
                ? new EmailStaffInvitationSender(
                    email,
                    provider.GetRequiredService<ClinicNameReader>(),
                    provider.GetRequiredService<IOptions<StaffInvitationOptions>>(),
                    provider.GetRequiredService<ILogger<EmailStaffInvitationSender>>())
                : provider.GetRequiredService<LoggingStaffInvitationSender>());

        return services;
    }

    private static IEmailTransport? Email(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<EmailOptions>>().Value.IsConfigured
            ? provider.GetRequiredService<SmtpEmailTransport>()
            : null;

    private static ISmsTransport? Sms(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<SmsOptions>>().Value.IsConfigured
            ? provider.GetRequiredService<ThaiBulkSmsTransport>()
            : null;
}
