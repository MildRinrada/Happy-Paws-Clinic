using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Clinics;

/// <summary>
/// Persistence port for the clinic's one-row settings: profile, opening hours, and the booking,
/// queue and medical-record rules.
/// <para>
/// One port for all five because they are the same shape — a fixed id, exactly one row, a default
/// when the row has never been written — and five near-identical repositories would be five places
/// to get "what if the row is missing" subtly different.
/// </para>
/// <para>
/// The two methods are the read/write split every repository here makes. <see cref="ReadAsync"/>
/// hands back an untracked row, or the type's default when there is none, and never writes.
/// <see cref="LoadForUpdateAsync"/> hands back a tracked row and adds the default to the unit of
/// work when there is none, so the first <c>PATCH</c> on a fresh instance creates the row it
/// changes.
/// </para>
/// </summary>
public interface IClinicSettingsStore
{
    Task<TSettings> ReadAsync<TSettings>(
        string id,
        Func<TSettings> createDefault,
        CancellationToken cancellationToken = default)
        where TSettings : Entity;

    Task<TSettings> LoadForUpdateAsync<TSettings>(
        string id,
        Func<TSettings> createDefault,
        CancellationToken cancellationToken = default)
        where TSettings : Entity;
}

/// <summary>
/// The five settings by name, so a handler asks for "the queue settings" rather than restating an
/// id and a factory that must agree with each other.
/// </summary>
public static class ClinicSettingsStoreExtensions
{
    public static Task<ClinicProfile> ProfileAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(ClinicProfile.SingletonId, ClinicProfile.CreateDefault, cancellationToken);

    public static Task<ClinicProfile> ProfileForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(ClinicProfile.SingletonId, ClinicProfile.CreateDefault, cancellationToken);

    public static Task<OpeningHours> OpeningHoursAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(OpeningHours.SingletonId, OpeningHours.CreateDefault, cancellationToken);

    public static Task<OpeningHours> OpeningHoursForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(OpeningHours.SingletonId, OpeningHours.CreateDefault, cancellationToken);

    public static Task<BookingSettings> BookingAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(BookingSettings.SingletonId, BookingSettings.CreateDefault, cancellationToken);

    public static Task<BookingSettings> BookingForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(BookingSettings.SingletonId, BookingSettings.CreateDefault, cancellationToken);

    public static Task<QueueSettings> QueueAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(QueueSettings.SingletonId, QueueSettings.CreateDefault, cancellationToken);

    public static Task<QueueSettings> QueueForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(QueueSettings.SingletonId, QueueSettings.CreateDefault, cancellationToken);

    public static Task<MedicalRecordSettings> MedicalRecordAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.ReadAsync(MedicalRecordSettings.SingletonId, MedicalRecordSettings.CreateDefault, cancellationToken);

    public static Task<MedicalRecordSettings> MedicalRecordForUpdateAsync(
        this IClinicSettingsStore store, CancellationToken cancellationToken = default) =>
        store.LoadForUpdateAsync(
            MedicalRecordSettings.SingletonId, MedicalRecordSettings.CreateDefault, cancellationToken);
}
