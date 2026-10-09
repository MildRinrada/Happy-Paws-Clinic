namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>
/// <c>Webhooks:*</c>.
/// <code>
/// "Webhooks": { "TimeoutSeconds": 10, "AllowLocalhostInDevelopment": true }
/// </code>
/// <para>
/// <see cref="TimeoutSeconds"/> bounds one attempt, connect to last byte, between 1 and 30.
/// <see cref="AllowLocalhostInDevelopment"/> lets a developer point a webhook at
/// <c>http://localhost</c>; it has no effect outside Development, where loopback is always refused.
/// Private, link-local and metadata addresses are refused everywhere and cannot be allowed.
/// </para>
/// </summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public int TimeoutSeconds { get; set; } = 10;

    public bool AllowLocalhostInDevelopment { get; set; } = true;
}
