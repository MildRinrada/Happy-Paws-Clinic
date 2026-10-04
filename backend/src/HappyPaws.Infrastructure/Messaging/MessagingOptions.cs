namespace HappyPaws.Infrastructure.Messaging;

/// <summary>
/// <c>Messaging:Email</c> — the clinic's own SMTP server. Any provider that speaks SMTP fits:
/// Google Workspace, Amazon SES, Resend, SendGrid, a hosting company's mailbox. Unset
/// <see cref="Host"/> and nothing is sent; the sign-in and invitation messages fall back to the
/// log placeholders.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Messaging:Email";

    public string? Host { get; init; }

    public int Port { get; init; } = 587;

    /// <summary><c>starttls</c> (port 587), <c>ssl</c> (port 465) or <c>none</c> (a relay on a private network only).</summary>
    public string Security { get; init; } = "starttls";

    public string? Username { get; init; }

    /// <summary>From the environment (<c>Messaging__Email__Password</c>), never from a committed file.</summary>
    public string? Password { get; init; }

    public string? FromAddress { get; init; }

    /// <summary>The name a recipient sees. Defaults to the clinic's own name from its profile.</summary>
    public string? FromName { get; init; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>
/// <c>Messaging:Sms</c> — the SMS gateway. <c>thaibulksms</c> is the one adapter today; an empty
/// provider sends nothing and the one-time codes fall back to the log placeholder.
/// </summary>
public sealed class SmsOptions
{
    public const string SectionName = "Messaging:Sms";

    public string? Provider { get; init; }

    public string? ApiKey { get; init; }

    /// <summary>From the environment (<c>Messaging__Sms__ApiSecret</c>), never from a committed file.</summary>
    public string? ApiSecret { get; init; }

    /// <summary>The sender name registered with the gateway. Optional; the gateway's default is used without it.</summary>
    public string? Sender { get; init; }

    public string BaseUrl { get; init; } = "https://api-v2.thaibulksms.com";

    public bool IsConfigured =>
        string.Equals(Provider, "thaibulksms", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
