using HappyPaws.Infrastructure.Messaging;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Messaging.Sms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Notifications.Routing;

/// <summary>The SMS gateway and mail server the customer messages use, or null where one is not set up.</summary>
internal interface IDeliveryTransports
{
    ISmsTransport? Sms { get; }

    IEmailTransport? Email { get; }
}

/// <summary>
/// The same transports the sign-in codes and staff invitations use (<c>Messaging:Sms</c>,
/// <c>Messaging:Email</c>), decided on each read from <see cref="IOptions{TOptions}"/> rather than
/// once at start-up, as <c>MessagingRegistration</c> does.
/// </summary>
internal sealed class ConfiguredDeliveryTransports(IServiceProvider provider) : IDeliveryTransports
{
    public ISmsTransport? Sms =>
        provider.GetRequiredService<IOptions<SmsOptions>>().Value.IsConfigured
            ? provider.GetRequiredService<ThaiBulkSmsTransport>()
            : null;

    public IEmailTransport? Email =>
        provider.GetRequiredService<IOptions<EmailOptions>>().Value.IsConfigured
            ? provider.GetRequiredService<SmtpEmailTransport>()
            : null;
}
