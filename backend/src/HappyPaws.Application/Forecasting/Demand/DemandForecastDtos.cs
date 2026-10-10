namespace HappyPaws.Application.Forecasting.Demand;

/// <summary>
/// <c>GET /staff/stats/demand-forecast</c>: expected patients per hour against vets on the roster.
/// Not in the API reference: <c>docs/contract-extensions.md § 33</c>.
/// </summary>
/// <param name="BranchId">The branch forecast.</param>
/// <param name="From">First day, today or later.</param>
/// <param name="To">Last day, included.</param>
/// <param name="HistoryWeeks">Weeks of history the walk-in averages and the high-load line come from.</param>
/// <param name="HighLoadPerVet">
/// The branch's 80th percentile of patients per vet per hour over those weeks; absent when the
/// history is too thin, and then only hours with no vet are flagged.
/// </param>
/// <param name="Days">Each day of the span, in order.</param>
public sealed record DemandForecastDto(
    string BranchId,
    DateOnly From,
    DateOnly To,
    int HistoryWeeks,
    double? HighLoadPerVet,
    IReadOnlyList<DemandDayDto> Days);

/// <summary>One day: its totals and its hours.</summary>
/// <param name="Date">The clinic day.</param>
/// <param name="BookedAppointments">Appointments already booked that day.</param>
/// <param name="ExpectedWalkIns">Walk-ins expected that day, summed over its hours.</param>
/// <param name="ExpectedTotal">The two together.</param>
/// <param name="HighLoadHours">How many of its hours are flagged.</param>
/// <param name="Hours">
/// Every hour with a vet on shift, a booking or walk-ins expected, earliest first. Hours with
/// none of the three are left out.
/// </param>
public sealed record DemandDayDto(
    DateOnly Date,
    int BookedAppointments,
    double ExpectedWalkIns,
    double ExpectedTotal,
    int HighLoadHours,
    IReadOnlyList<DemandHourDto> Hours);

/// <summary>One clinic hour.</summary>
/// <param name="Hour">0 to 23, clinic time.</param>
/// <param name="StartsAt">The hour's start as an instant.</param>
/// <param name="BookedAppointments">Booked or confirmed appointments starting in the hour (walk-ins excluded).</param>
/// <param name="ExpectedWalkIns">Average walk-in tickets of the same weekday and hour, two decimals.</param>
/// <param name="ExpectedTotal">The two together.</param>
/// <param name="VetsOnShift">Vets whose shift overlaps the hour.</param>
/// <param name="LoadPerVet">Expected patients per vet; absent when no vet is on shift.</param>
/// <param name="HighLoad">Above the branch's line, or patients expected with no vet on shift.</param>
public sealed record DemandHourDto(
    int Hour,
    DateTimeOffset StartsAt,
    int BookedAppointments,
    double ExpectedWalkIns,
    double ExpectedTotal,
    int VetsOnShift,
    double? LoadPerVet,
    bool HighLoad);
