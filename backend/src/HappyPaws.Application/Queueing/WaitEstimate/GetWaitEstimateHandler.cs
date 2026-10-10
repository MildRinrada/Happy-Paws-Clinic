using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Queueing.WaitEstimate;

/// <summary><c>GET /queue/wait-estimate?branch=</c>.</summary>
/// <param name="BranchId">One open branch; may be omitted when the clinic has only one.</param>
public sealed record GetWaitEstimateQuery(string? BranchId = null);

/// <summary>
/// "ตอนนี้รอประมาณกี่นาที" for a walk-in, open to anyone. The rules are <see cref="WaitEstimateRules"/>;
/// this handler picks the branch, reads three counts, and reuses the answer for thirty seconds.
/// <para>
/// The branch rule is the public booking feed's: the branch asked for when it is open, otherwise
/// the clinic's only open branch, otherwise <c>422</c> at <c>branch</c>. An unknown or closed-down
/// branch is the same <c>422</c>, so the endpoint cannot be used to discover which ids exist.
/// </para>
/// <para>
/// Not in the API reference: <c>docs/contract-extensions.md § 37</c>.
/// </para>
/// </summary>
public sealed class GetWaitEstimateHandler(
    IBranchRepository branches,
    IClinicSettingsStore settings,
    IHolidayRepository holidays,
    IWaitEstimateQueries queries,
    WaitEstimateCache cache,
    IClock clock)
{
    /// <summary>Holidays read ahead; the look-ahead is two weeks, so this is every one that matters.</summary>
    private const int MaxHolidays = 100;

    public async Task<WaitEstimateDto> HandleAsync(GetWaitEstimateQuery query, CancellationToken cancellationToken = default)
    {
        var branchId = await BranchAsync(query.BranchId, cancellationToken);

        if (cache.Fresh(branchId) is { } cached)
        {
            return cached;
        }

        var now = clock.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, clock.ClinicTimeZone).DateTime;
        var today = DateOnly.FromDateTime(localNow);

        var hours = await settings.OpeningHoursAsync(cancellationToken);
        var closures = await holidays.FromAsync(today, MaxHolidays, cancellationToken);
        var waiting = await queries.WaitingAsync(branchId, today, cancellationToken);
        var vets = await queries.VetsOnDutyAsync(branchId, now, cancellationToken);

        WaitEstimateDto estimate;

        if (WaitEstimateRules.IsOpen(hours, closures, branchId, localNow, vets))
        {
            var timed = await queries.ConsultationTimeAsync(
                branchId, now.AddDays(-WaitEstimateRules.HistoryDays), now, cancellationToken);
            var minutes = WaitEstimateRules.ConsultationMinutes(timed.Samples, timed.MedianMinutes);
            var (low, high) = WaitEstimateRules.Range(waiting, minutes, vets);

            estimate = new WaitEstimateDto(branchId, true, waiting, vets, low, high, null, now);
        }
        else
        {
            var opensAt = WaitEstimateRules.NextOpening(hours, closures, branchId, localNow) is { } local
                ? new DateTimeOffset(local, clock.ClinicTimeZone.GetUtcOffset(local))
                : (DateTimeOffset?)null;

            estimate = new WaitEstimateDto(branchId, false, waiting, vets, null, null, opensAt, now);
        }

        cache.Store(estimate);

        return estimate;
    }

    private async Task<string> BranchAsync(string? requested, CancellationToken cancellationToken)
    {
        var open = await branches.ListActiveAsync(cancellationToken);

        if (requested is { Length: > 0 })
        {
            var branchId = requested.Trim();

            return open.Any(branch => branch.Id == branchId)
                ? branchId
                : throw Invalid("ไม่พบสาขาที่เปิดให้บริการนี้");
        }

        return open.Count == 1 ? open[0].Id : throw Invalid("ต้องระบุสาขา");
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["branch"] = [message] });
}
