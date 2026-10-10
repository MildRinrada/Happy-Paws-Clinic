namespace HappyPaws.Application.Queueing.WaitEstimate;

/// <summary>
/// <c>GET /queue/wait-estimate</c>: about how long a walk-in waits at one branch right now. Counts
/// only. Not in the API reference: <c>docs/contract-extensions.md § 37</c>.
/// </summary>
/// <param name="BranchId">The branch estimated.</param>
/// <param name="OpenNow">Whether the branch is open now (hours, holidays, clinic time zone).</param>
/// <param name="Waiting">Tickets still waiting to be called there today.</param>
/// <param name="VetsOnDuty">Vets on shift there now.</param>
/// <param name="EstimateMinutesLow">The low end of the wait, in minutes; absent when closed.</param>
/// <param name="EstimateMinutesHigh">The high end, ten minutes more; absent when closed.</param>
/// <param name="OpensAt">When a closed branch opens next, within two weeks; absent when open or unknown.</param>
/// <param name="UpdatedAt">When these figures were worked out. Reused for up to 30 seconds.</param>
public sealed record WaitEstimateDto(
    string BranchId,
    bool OpenNow,
    int Waiting,
    int VetsOnDuty,
    int? EstimateMinutesLow,
    int? EstimateMinutesHigh,
    DateTimeOffset? OpensAt,
    DateTimeOffset UpdatedAt);
