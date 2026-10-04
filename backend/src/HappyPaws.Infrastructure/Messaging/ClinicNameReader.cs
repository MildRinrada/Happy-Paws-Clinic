using HappyPaws.Application.Clinics;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Infrastructure.Messaging;

/// <summary>
/// The clinic's name for the top of a message, read from its profile. The senders are singletons
/// and the profile lives behind a scoped store, so each read takes a scope of its own.
/// </summary>
internal sealed class ClinicNameReader(IServiceScopeFactory scopes)
{
    public async Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var profile = await scope.ServiceProvider.GetRequiredService<IClinicSettingsStore>().ProfileAsync(cancellationToken);

        return profile.Name;
    }
}
