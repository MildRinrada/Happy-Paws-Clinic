using HappyPaws.Application.Integrations.ConfigureLabProvider;
using HappyPaws.Application.Integrations.ConfigurePaymentProvider;
using HappyPaws.Application.Integrations.CreateApiKey;
using HappyPaws.Application.Integrations.CreateWebhookSubscription;
using HappyPaws.Application.Integrations.UpdateWebhookSubscription;

namespace HappyPaws.Api.Contracts.Integrations;

/// <summary>
/// <c>POST /staff/webhooks</c>:
/// <code>{ "url": "https://example.com/hooks/clinic", "events": ["appointment.created", "invoice.issued"], "description": "ระบบบัญชี" }</code>
/// </summary>
public sealed record CreateWebhookRequest(
    string? Url = null,
    IReadOnlyList<string>? Events = null,
    string? Description = null)
{
    public CreateWebhookSubscriptionCommand ToCommand() => new(Url ?? string.Empty, Events ?? [], Description);
}

/// <summary>
/// <c>PATCH /staff/webhooks/{id}</c> (<c>docs/contract-extensions.md § 22</c>). Every field
/// optional; an empty <c>description</c> clears it.
/// <code>{ "url": "https://example.com/hooks/v2", "events": ["invoice.issued"], "description": "ระบบบัญชี", "active": false }</code>
/// </summary>
public sealed record UpdateWebhookRequest(
    string? Url = null,
    IReadOnlyList<string>? Events = null,
    string? Description = null,
    bool? Active = null)
{
    public UpdateWebhookSubscriptionCommand ToCommand(string id, uint? ifMatch) =>
        new(id, Url, Events, Description, Active, ifMatch);
}

/// <summary>
/// <c>POST /staff/api-keys</c>:
/// <code>{ "name": "ระบบบัญชี", "scopes": ["appointment:read", "customer:read"], "branch_ids": ["brn_…"], "expires_at": "2027-01-01T00:00:00+07:00" }</code>
/// </summary>
public sealed record CreateApiKeyRequest(
    string? Name = null,
    IReadOnlyList<string>? Scopes = null,
    IReadOnlyList<string>? BranchIds = null,
    DateTimeOffset? ExpiresAt = null)
{
    public CreateApiKeyCommand ToCommand() => new(Name ?? string.Empty, Scopes ?? [], BranchIds, ExpiresAt);
}

/// <summary>
/// <c>PUT /staff/integrations/payment</c>. Every field optional; an empty string clears.
/// <code>{ "provider": "opn", "mode": "test", "enabled": false, "public_key": "pkey_test_…", "secret_key": "skey_test_…", "webhook_secret": "…" }</code>
/// </summary>
public sealed record ConfigurePaymentRequest(
    string? Provider = null,
    string? Mode = null,
    bool? Enabled = null,
    string? PublicKey = null,
    string? SecretKey = null,
    string? WebhookSecret = null)
{
    public ConfigurePaymentProviderCommand ToCommand(uint? ifMatch) =>
        new(Provider, Mode, Enabled, PublicKey, SecretKey, WebhookSecret, ifMatch);
}

/// <summary>
/// <c>PUT /staff/integrations/lab</c>. Every field optional; an empty string clears.
/// <code>{ "provider": "idexx", "enabled": false, "account_id": "TH-12345", "base_url": "https://api.lab.example", "api_key": "…" }</code>
/// </summary>
public sealed record ConfigureLabRequest(
    string? Provider = null,
    bool? Enabled = null,
    string? AccountId = null,
    string? BaseUrl = null,
    string? ApiKey = null)
{
    public ConfigureLabProviderCommand ToCommand(uint? ifMatch) => new(Provider, Enabled, AccountId, BaseUrl, ApiKey, ifMatch);
}
