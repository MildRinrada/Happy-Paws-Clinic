using HappyPaws.Application.Common;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Dtos;

/// <summary>
/// <c>PUT /staff/integrations/line</c> — "channel id/secret · เปิดปิดชนิดการแจ้งเตือน".
/// <para>
/// <b>No secret is ever in here.</b> <c>HasChannelSecret</c> and <c>HasChannelAccessToken</c> say
/// whether one is stored; <c>SecretSource</c> and <c>TokenSource</c> say which one is in use
/// (<c>database</c>, <c>configuration</c> or <c>none</c>). <c>NotificationTypes</c> lists every
/// template with whether it may go out on LINE. <c>WebhookPath</c> is the path, under the API's
/// base URL, to give LINE as the webhook.
/// </para>
/// </summary>
public sealed record LineSettingsDto(
    uint Version,
    string? ChannelId,
    bool Enabled,
    bool HasChannelSecret,
    bool HasChannelAccessToken,
    string SecretSource,
    string TokenSource,
    bool CanPush,
    bool CanVerify,
    IReadOnlyDictionary<string, bool> NotificationTypes,
    int BroadcastMonthlyLimit,
    string WebhookPath,
    DateTimeOffset? UpdatedAt)
{
    public const string Webhook = "/webhooks/line";

    public static LineSettingsDto From(LineChannelSettings settings, LineConnection connection) =>
        new(
            settings.Version,
            connection.ChannelId,
            settings.Enabled,
            settings.ChannelSecret is not null,
            settings.ChannelAccessToken is not null,
            WireEnum.Of(connection.SecretSource),
            WireEnum.Of(connection.TokenSource),
            connection.CanPush,
            connection.CanVerify,
            NotificationTemplateCatalogue.All.ToDictionary(
                template => template.Key,
                template => settings.IsTemplateEnabled(template.Key),
                StringComparer.Ordinal),
            settings.BroadcastMonthlyLimit,
            Webhook,
            settings.UpdatedAt ?? (settings.CreatedAt == default ? null : settings.CreatedAt));
}
