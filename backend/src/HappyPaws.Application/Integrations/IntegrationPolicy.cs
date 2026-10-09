namespace HappyPaws.Application.Integrations;

/// <summary>
/// Deployment facts the integration handlers need, decided once at start-up from the host
/// environment and <c>Webhooks:*</c> configuration.
/// </summary>
/// <param name="AllowLocalhost">
/// True only in Development (and only while <c>Webhooks:AllowLocalhostInDevelopment</c> is not
/// false): webhook and lab URLs may then be <c>http://localhost</c>, and the sender may connect to
/// loopback. Never private or link-local ranges, in any environment.
/// </param>
/// <param name="Timeout">How long one webhook attempt may take, connect to last byte.</param>
public sealed record IntegrationPolicy(bool AllowLocalhost, TimeSpan Timeout)
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
}
