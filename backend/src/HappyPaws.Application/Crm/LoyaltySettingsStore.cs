using HappyPaws.Application.Clinics;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm;

/// <summary>
/// The points programme, read and written through the same one-row settings store as the clinic's
/// other rules. A clinic that never saved it reads the defaults, at version 0.
/// </summary>
public static class LoyaltySettingsStore
{
    public static Task<LoyaltySettings> LoyaltyAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(LoyaltySettings.SingletonId, LoyaltySettings.CreateDefault, cancellationToken);

    public static Task<LoyaltySettings> LoyaltyForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(LoyaltySettings.SingletonId, LoyaltySettings.CreateDefault, cancellationToken);
}
