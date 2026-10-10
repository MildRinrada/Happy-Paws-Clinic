using System.Globalization;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// The monthly counter behind the AI allowance, in the database.
/// <para>
/// Unlike the website assistant's in-memory day counter, this one survives restarts and is shared
/// by every API instance: it is what the vendor's bill is capped by. One row per month and feature,
/// plus the month's total, in <c>staff_ai_usage</c>.
/// </para>
/// </summary>
public interface IStaffAiUsageStore
{
    /// <summary>
    /// Counts one use against <paramref name="month"/> if the month's total is still under
    /// <paramref name="limit"/>, in one statement, and says whether it did. Two requests racing for
    /// the last use cannot both get it.
    /// </summary>
    Task<bool> TryReserveAsync(string month, string feature, int limit, CancellationToken cancellationToken = default);

    /// <summary>Gives one use back, never going below zero.</summary>
    Task ReleaseAsync(string month, string feature, CancellationToken cancellationToken = default);

    /// <summary>The month's total and per-feature counts. Zeroes for a month with no use.</summary>
    Task<StaffAiUsageCounts> ReadAsync(string month, CancellationToken cancellationToken = default);
}

public sealed record StaffAiUsageCounts(int Used, IReadOnlyDictionary<string, int> ByFeature);

/// <summary>A use counted before the model was called, so it can be given back if the call fails.</summary>
public sealed record StaffAiReservation(string Month, string Feature);

/// <summary>
/// Reserves a use before the model is called and refuses with <c>429 ai_quota_exceeded</c> once the
/// month is spent.
/// <para>
/// <b>Months are clinic months</b> (<c>Asia/Bangkok</c>): the allowance renews at midnight on the
/// first in Bangkok, not at 07:00 when UTC turns over.
/// </para>
/// <para>
/// <b>A refund goes to the month it was taken from.</b> A call that started at 23:59:59 on the
/// last day and failed after midnight gives the use back to the old month, not the new one.
/// </para>
/// </summary>
public sealed class StaffAiQuota(IStaffAiUsageStore store, StaffAiSettings settings, IClock clock)
{
    /// <summary><c>2026-10</c>: the clinic month an instant in clinic time falls in.</summary>
    public static string MonthOf(DateTimeOffset clinicTime) =>
        clinicTime.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public string CurrentMonth => MonthOf(clock.NowInClinicTime());

    public async Task<StaffAiReservation> ReserveAsync(string feature, CancellationToken cancellationToken = default)
    {
        var month = CurrentMonth;

        if (settings.MonthlyCalls <= 0 || !await store.TryReserveAsync(month, feature, settings.MonthlyCalls, cancellationToken))
        {
            throw new AiQuotaExceededException(month, Math.Max(settings.MonthlyCalls, 0));
        }

        return new StaffAiReservation(month, feature);
    }

    public Task RefundAsync(StaffAiReservation reservation, CancellationToken cancellationToken = default) =>
        store.ReleaseAsync(reservation.Month, reservation.Feature, cancellationToken);
}
