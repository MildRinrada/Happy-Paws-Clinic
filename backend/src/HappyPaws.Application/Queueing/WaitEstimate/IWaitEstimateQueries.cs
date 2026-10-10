namespace HappyPaws.Application.Queueing.WaitEstimate;

/// <summary>
/// The three counts behind <c>GET /queue/wait-estimate</c> (<c>docs/contract-extensions.md § 37</c>).
/// Hand-written SQL in Infrastructure like the forecasts; nothing here reads a name, a number or an id
/// of a ticket, only how many.
/// </summary>
public interface IWaitEstimateQueries
{
    /// <summary>Tickets at the branch on that service day still waiting to be called (<c>waiting</c> or <c>skipped</c>).</summary>
    Task<int> WaitingAsync(string branchId, DateOnly serviceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tickets served at the branch in <c>[from, to)</c> with both room times, and the median minutes
    /// from entering the room to being served. A visit of four hours or more is left out as a
    /// consultation someone forgot to close.
    /// </summary>
    Task<ConsultationTimeRow> ConsultationTimeAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct vets with an active (not cancelled) shift at the branch covering <paramref name="at"/>.</summary>
    Task<int> VetsOnDutyAsync(string branchId, DateTimeOffset at, CancellationToken cancellationToken = default);
}

/// <param name="Samples">Timed consultations counted.</param>
/// <param name="MedianMinutes">Their median length; null when there were none.</param>
public sealed record ConsultationTimeRow(int Samples, double? MedianMinutes);
