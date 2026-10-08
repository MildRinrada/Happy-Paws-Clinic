using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ConfigureLineChannel;

/// <summary>
/// <c>PUT /staff/integrations/line</c> — "channel id/secret · เปิดปิดชนิดการแจ้งเตือน". Every field
/// is optional and an absent one is left as it is, so a secret does not have to be typed again to
/// switch a notification type off. An empty string clears a stored credential. An empty body
/// writes nothing and returns the settings as they stand, because the contract has no
/// <c>GET</c> (the same reading as the queue settings, <c>docs/assumptions.md § 27</c>).
/// <c>NotificationTypes</c> changes only the templates it names.
/// </summary>
public sealed record ConfigureLineChannelCommand(
    string? ChannelId = null,
    string? ChannelSecret = null,
    string? ChannelAccessToken = null,
    bool? Enabled = null,
    IReadOnlyDictionary<string, bool>? NotificationTypes = null,
    int? BroadcastMonthlyLimit = null,
    uint? IfMatchVersion = null)
{
    public bool IsEmpty =>
        ChannelId is null && ChannelSecret is null && ChannelAccessToken is null && Enabled is null
        && NotificationTypes is null && BroadcastMonthlyLimit is null;
}

public sealed class ConfigureLineChannelValidator : AbstractValidator<ConfigureLineChannelCommand>
{
    public ConfigureLineChannelValidator()
    {
        RuleFor(command => command.ChannelId)
            .Must(id => id!.Trim().Length <= 20 && id.Trim().All(char.IsAsciiDigit))
            .WithMessage("channel id ต้องเป็นตัวเลขไม่เกิน 20 หลัก")
            .When(command => command.ChannelId is not null);

        RuleFor(command => command.ChannelSecret)
            .Must(secret => secret!.Trim().Length <= 200 && !secret.Trim().Any(char.IsWhiteSpace))
            .WithMessage("channel secret ไม่ถูกต้อง")
            .When(command => command.ChannelSecret is not null);

        RuleFor(command => command.ChannelAccessToken)
            .Must(token => token!.Trim().Length <= 1_000 && !token.Trim().Any(char.IsWhiteSpace))
            .WithMessage("channel access token ไม่ถูกต้อง")
            .When(command => command.ChannelAccessToken is not null);

        RuleFor(command => command.BroadcastMonthlyLimit)
            .InclusiveBetween(0, LineChannelSettings.BroadcastMonthlyLimitMax)
            .When(command => command.BroadcastMonthlyLimit is not null);

        RuleFor(command => command.NotificationTypes)
            .Must(types => types!.Keys.All(NotificationTemplateCatalogue.IsKnown))
            .WithMessage(command =>
                "ไม่รู้จักชนิดการแจ้งเตือน: "
                + string.Join(", ", command.NotificationTypes!.Keys.Where(key => !NotificationTemplateCatalogue.IsKnown(key))))
            .When(command => command.NotificationTypes is not null);
    }
}

/// <summary>
/// Sets the clinic's LINE channel: its id and secret, the access token that pushes, the master
/// switch, which notification types may go out on LINE, and the monthly broadcast quota.
/// <para>
/// Audited, with which fields changed and never their values. The access token can message every
/// follower in the clinic's name, so who replaced it, and when, is the first question after a
/// message nobody at the clinic wrote.
/// </para>
/// </summary>
public sealed class ConfigureLineChannelHandler(
    IClinicSettingsStore settings,
    ILineGateway line,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<LineSettingsDto> HandleAsync(
        ConfigureLineChannelCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command.IsEmpty)
        {
            var current = await settings.ReadAsync(LineChannelSettings.SingletonId, LineChannelSettings.CreateDefault, cancellationToken);
            return LineSettingsDto.From(current, await line.ConnectionAsync(cancellationToken));
        }

        LineChannelSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await settings.LoadForUpdateAsync(
                LineChannelSettings.SingletonId, LineChannelSettings.CreateDefault, token);

            ConcurrencyGuard.Ensure(record, command.IfMatchVersion);

            record.Update(
                command.ChannelId,
                command.ChannelSecret,
                command.ChannelAccessToken,
                command.Enabled,
                DisabledAfter(record, command.NotificationTypes),
                command.BroadcastMonthlyLimit);

            audit.Record(new AuditEvent(
                AuditActions.LineIntegrationConfigured,
                AuditResources.Integration,
                record.Id,
                Details: new Dictionary<string, object?>
                {
                    ["channel_id"] = record.ChannelId,
                    ["channel_secret_changed"] = command.ChannelSecret is not null,
                    ["channel_access_token_changed"] = command.ChannelAccessToken is not null,
                    ["enabled"] = record.Enabled,
                    ["disabled_templates"] = record.DisabledTemplates.ToArray(),
                    ["broadcast_monthly_limit"] = record.BroadcastMonthlyLimit,
                }));

            updated = record;
        }, cancellationToken);

        return LineSettingsDto.From(updated!, await line.ConnectionAsync(cancellationToken));
    }

    /// <summary>The disabled set after applying the switches named in the request, and only those.</summary>
    private static List<string>? DisabledAfter(LineChannelSettings record, IReadOnlyDictionary<string, bool>? switches)
    {
        if (switches is null)
        {
            return null;
        }

        var disabled = record.DisabledTemplates.ToHashSet(StringComparer.Ordinal);

        foreach (var (key, on) in switches)
        {
            if (on)
            {
                disabled.Remove(key);
            }
            else
            {
                disabled.Add(key);
            }
        }

        return [.. disabled];
    }
}
