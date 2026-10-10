using System.Collections.Concurrent;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Queueing.WaitEstimate;

/// <summary>
/// The last estimate per branch, reused for <see cref="WaitEstimateRules.CacheSeconds"/>. A singleton
/// in memory: one deployment serves one clinic, so a branch id is the whole key, and a restart only
/// costs one fresh read. Two requests arriving together may both work it out; the later one wins,
/// which is harmless for a figure this rough.
/// </summary>
public sealed class WaitEstimateCache(IClock clock)
{
    private readonly ConcurrentDictionary<string, WaitEstimateDto> _entries = new(StringComparer.Ordinal);

    /// <summary>The cached estimate for the branch, while it is still fresh.</summary>
    public WaitEstimateDto? Fresh(string branchId) =>
        _entries.TryGetValue(branchId, out var entry)
            && clock.UtcNow - entry.UpdatedAt < TimeSpan.FromSeconds(WaitEstimateRules.CacheSeconds)
            ? entry
            : null;

    public void Store(WaitEstimateDto estimate) => _entries[estimate.BranchId] = estimate;
}
