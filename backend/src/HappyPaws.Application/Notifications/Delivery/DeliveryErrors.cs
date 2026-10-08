namespace HappyPaws.Application.Notifications.Delivery;

/// <summary>
/// The reasons a delivery-log row gives for a message that did not go out. Clients switch on
/// these, so they are part of the contract of <c>GET /staff/notifications/log</c>.
/// </summary>
public static class DeliveryErrors
{
    /// <summary>The customer has not agreed to this kind of message. Nothing was attempted.</summary>
    public const string NoConsent = "no_consent";

    /// <summary>The customer record could not be read.</summary>
    public const string NoRecipient = "no_recipient";

    /// <summary>The customer has no address for the channel asked for.</summary>
    public const string NoAddress = "no_address";

    /// <summary>The channel's provider is not set up on this instance.</summary>
    public const string NotConfigured = "channel_not_configured";

    /// <summary>LINE's master switch is off.</summary>
    public const string ChannelDisabled = "channel_disabled";

    /// <summary>This kind of message is switched off for LINE.</summary>
    public const string TemplateDisabled = "template_disabled";

    /// <summary>The customer turned this channel off in their account.</summary>
    public const string OptedOut = "opted_out";

    /// <summary>The customer blocked the clinic's LINE account.</summary>
    public const string Unfollowed = "unfollowed";

    /// <summary>Automatic routing found no channel able to carry the message.</summary>
    public const string NoChannel = "no_channel";

    /// <summary>The provider refused the message itself (bad number, bad address, bad payload).</summary>
    public const string ProviderRejected = "provider_rejected";

    /// <summary>The provider refused the credentials.</summary>
    public const string ProviderUnauthorized = "provider_unauthorized";

    /// <summary>The provider's rate limit or monthly quota is used up.</summary>
    public const string ProviderQuota = "provider_quota";

    /// <summary>The provider failed for its own reasons.</summary>
    public const string ProviderError = "provider_error";

    /// <summary>The provider could not be reached in time.</summary>
    public const string ProviderUnreachable = "provider_unreachable";
}
