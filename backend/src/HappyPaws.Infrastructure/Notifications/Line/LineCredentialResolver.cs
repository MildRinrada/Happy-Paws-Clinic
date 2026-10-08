using HappyPaws.Application.Clinics;
using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Integrations;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Notifications.Line;

/// <summary>The LINE credentials in force for this request, and the stored settings they came with.</summary>
internal sealed record LineCredentials(
    LineChannelSettings Settings,
    string? ChannelId,
    string? ChannelSecret,
    string? AccessToken,
    LineCredentialSource SecretSource,
    LineCredentialSource TokenSource)
{
    public bool CanPush => AccessToken is not null && Settings.Enabled;

    public LineConnection ToConnection() =>
        new(CanPush, ChannelSecret is not null, Settings.Enabled, ChannelId, TokenSource, SecretSource);
}

/// <summary>
/// Puts together the stored LINE settings and configuration: a credential stored with
/// <c>PUT /staff/integrations/line</c> wins, configuration fills what is not stored. Read once per
/// request scope, so a send to fifty customers does not read the settings row fifty times.
/// </summary>
internal sealed class LineCredentialResolver(IClinicSettingsStore settings, IOptions<LineOptions> options)
{
    private LineCredentials? _resolved;

    public async Task<LineCredentials> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        var stored = await settings.ReadAsync(LineChannelSettings.SingletonId, LineChannelSettings.CreateDefault, cancellationToken);
        var configured = options.Value;

        var (secret, secretSource) = Pick(stored.ChannelSecret, configured.ChannelSecret);
        var (token, tokenSource) = Pick(stored.ChannelAccessToken, configured.ChannelAccessToken);

        _resolved = new LineCredentials(
            stored,
            stored.ChannelId ?? Blank(configured.ChannelId),
            secret,
            token,
            secretSource,
            tokenSource);

        return _resolved;
    }

    private static (string? Value, LineCredentialSource Source) Pick(string? stored, string? configured) =>
        stored is not null
            ? (stored, LineCredentialSource.Database)
            : Blank(configured) is { } fromConfiguration
                ? (fromConfiguration, LineCredentialSource.Configuration)
                : (null, LineCredentialSource.None);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
