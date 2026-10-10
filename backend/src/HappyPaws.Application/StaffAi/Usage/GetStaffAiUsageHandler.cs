namespace HappyPaws.Application.StaffAi.Usage;

/// <summary>
/// <c>GET /staff/ai/usage</c>: whether the assistant is on, and this clinic month's allowance and
/// use. <c>by_feature</c> always has every feature, zero when unused. The page hides every AI
/// button when <c>enabled</c> is false.
/// </summary>
public sealed record StaffAiUsageDto(
    bool Enabled,
    string Month,
    int Limit,
    int Used,
    IReadOnlyDictionary<string, int> ByFeature);

public sealed class GetStaffAiUsageHandler(IStaffAiUsageStore store, StaffAiQuota quota, StaffAiGateway gateway)
{
    public async Task<StaffAiUsageDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var month = quota.CurrentMonth;
        var counts = await store.ReadAsync(month, cancellationToken);

        return new StaffAiUsageDto(
            gateway.IsEnabled,
            month,
            Math.Max(gateway.Settings.MonthlyCalls, 0),
            counts.Used,
            StaffAiFeatures.All.ToDictionary(feature => feature, feature => counts.ByFeature.GetValueOrDefault(feature)));
    }
}
