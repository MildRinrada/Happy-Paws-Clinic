using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Forecasting.Demand;

/// <summary><c>GET /staff/stats/demand-forecast?branch=&amp;from=&amp;to=</c>.</summary>
/// <param name="BranchId">One branch; the caller's only branch, or the clinic's only one, when absent.</param>
/// <param name="From">First clinic day, today or later; today when absent.</param>
/// <param name="To">Last clinic day, included; a week from <paramref name="From"/> when absent. At most 14 days.</param>
public sealed record GetDemandForecastQuery(string? BranchId = null, DateOnly? From = null, DateOnly? To = null);

/// <summary>
/// "วันไหนชั่วโมงไหนจะยุ่ง และหมอพอไหม": booked appointments, walk-ins expected from the same
/// weekday and hour of the last eight weeks, and vets on the roster, hour by hour, with the hours
/// that look heavy for the vets in flagged. The rules are <see cref="DemandForecastRules"/>.
/// <para>
/// Days and hours are clinic days and hours, cut in the clinic's zone exactly as the statistics
/// endpoints cut them (<see cref="ClinicDays"/> here, <c>AT TIME ZONE</c> in the SQL). Not in the API
/// reference: <c>docs/contract-extensions.md § 33</c>.
/// </para>
/// </summary>
public sealed class GetDemandForecastHandler(
    IForecastQueries queries,
    ForecastBranches branches,
    ClinicDays clinicDays,
    IClock clock)
{
    public async Task<DemandForecastDto> HandleAsync(GetDemandForecastQuery query, CancellationToken cancellationToken = default)
    {
        var today = clinicDays.Today;
        var from = query.From ?? today;
        var to = query.To ?? from.AddDays(DemandForecastRules.DefaultSpanDays - 1);

        if (from < today)
        {
            throw Invalid("from", "คาดการณ์ได้ตั้งแต่วันนี้เป็นต้นไป");
        }

        if (to < from)
        {
            throw Invalid("to", "วันสุดท้ายต้องไม่อยู่ก่อนวันแรก");
        }

        if (to.DayNumber - from.DayNumber + 1 > DemandForecastRules.MaxSpanDays)
        {
            throw Invalid("to", $"คาดการณ์ได้ครั้งละไม่เกิน {DemandForecastRules.MaxSpanDays} วัน");
        }

        var branchId = await branches.OneAsync(query.BranchId, cancellationToken);

        var historyFirst = today.AddDays(-7 * DemandForecastRules.HistoryWeeks);
        var historyLast = today.AddDays(-1);
        var windowFrom = clinicDays.StartOf(historyFirst);
        var windowTo = clinicDays.StartOf(to.AddDays(1));

        var booked = Index(await queries.AppointmentsByHourAsync(branchId, windowFrom, windowTo, cancellationToken));
        var walkIns = Index(await queries.WalkInsByHourAsync(branchId, historyFirst, historyLast, cancellationToken));
        var firstTicket = await queries.FirstTicketDayAsync(branchId, cancellationToken);
        var vets = DemandForecastRules.VetsByHour(
            await queries.VetShiftsAsync(branchId, windowFrom, windowTo, cancellationToken),
            clock.ClinicTimeZone);

        int Count(Dictionary<(DateOnly, int), int> counts, DateOnly day, int hour) =>
            counts.GetValueOrDefault((day, hour));

        // The branch's own normal: patients per vet in each past hour that had both.
        var loads = new List<double>();

        for (var day = historyFirst; day <= historyLast; day = day.AddDays(1))
        {
            for (var hour = 0; hour < 24; hour++)
            {
                var onShift = Count(vets, day, hour);
                var seen = Count(booked, day, hour) + Count(walkIns, day, hour);

                if (onShift > 0 && seen > 0)
                {
                    loads.Add(seen / (double)onShift);
                }
            }
        }

        var threshold = DemandForecastRules.Threshold(loads);
        var days = new List<DemandDayDto>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var samples = DemandForecastRules.SampleDays(day, today, firstTicket);
            var hours = new List<DemandHourDto>();

            for (var hour = 0; hour < 24; hour++)
            {
                var appointments = Count(booked, day, hour);
                var expectedWalkIns = DemandForecastRules.Average(samples, sample => Count(walkIns, sample, hour));
                var onShift = Count(vets, day, hour);

                if (appointments == 0 && expectedWalkIns == 0 && onShift == 0)
                {
                    continue;
                }

                var expected = appointments + expectedWalkIns;

                hours.Add(new DemandHourDto(
                    hour,
                    HourStart(day, hour),
                    appointments,
                    DemandForecastRules.Round(expectedWalkIns),
                    DemandForecastRules.Round(expected),
                    onShift,
                    DemandForecastRules.LoadPerVet(expected, onShift) is { } load ? DemandForecastRules.Round(load) : null,
                    DemandForecastRules.IsHighLoad(expected, onShift, threshold)));
            }

            days.Add(new DemandDayDto(
                day,
                hours.Sum(row => row.BookedAppointments),
                DemandForecastRules.Round(hours.Sum(row => row.ExpectedWalkIns)),
                DemandForecastRules.Round(hours.Sum(row => row.ExpectedTotal)),
                hours.Count(row => row.HighLoad),
                hours));
        }

        return new DemandForecastDto(
            branchId,
            from,
            to,
            DemandForecastRules.HistoryWeeks,
            threshold is { } line ? DemandForecastRules.Round(line) : null,
            days);
    }

    private DateTimeOffset HourStart(DateOnly day, int hour)
    {
        var local = day.ToDateTime(new TimeOnly(hour, 0));

        return new DateTimeOffset(local, clock.ClinicTimeZone.GetUtcOffset(local));
    }

    private static Dictionary<(DateOnly, int), int> Index(IEnumerable<HourCountRow> rows) =>
        rows.GroupBy(row => (row.Day, row.Hour)).ToDictionary(group => group.Key, group => group.Sum(row => row.Count));

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
