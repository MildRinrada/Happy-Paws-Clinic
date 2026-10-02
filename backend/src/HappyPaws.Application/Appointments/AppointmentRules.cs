using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// The two Tier-2 rule sets of the booking module — appointment types and the cancellation policy —
/// read and written through the same one-row settings store as the clinic's other rules.
/// </summary>
public static class AppointmentRules
{
    public static Task<AppointmentTypeCatalog> AppointmentTypesAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(AppointmentTypeCatalog.SingletonId, AppointmentTypeCatalog.CreateDefault, cancellationToken);

    public static Task<AppointmentTypeCatalog> AppointmentTypesForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(AppointmentTypeCatalog.SingletonId, AppointmentTypeCatalog.CreateDefault, cancellationToken);

    public static Task<CancellationPolicy> CancellationPolicyAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(CancellationPolicy.SingletonId, CancellationPolicy.CreateDefault, cancellationToken);

    public static Task<CancellationPolicy> CancellationPolicyForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(CancellationPolicy.SingletonId, CancellationPolicy.CreateDefault, cancellationToken);

    /// <summary>
    /// The type a booking asked for, or <c>422</c> on <c>type</c> when the clinic has no such type.
    /// Null or empty means <c>regular</c>.
    /// </summary>
    public static async Task<AppointmentType> RequireTypeAsync(
        this IClinicSettingsStore store,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var catalog = await store.AppointmentTypesAsync(cancellationToken);

        return catalog.Find(code)
            ?? throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["type"] = [$"ไม่มีประเภทนัด '{code}' ประเภทที่มีคือ {string.Join(", ", catalog.Types.Select(type => type.Code))}"],
            });
    }
}
