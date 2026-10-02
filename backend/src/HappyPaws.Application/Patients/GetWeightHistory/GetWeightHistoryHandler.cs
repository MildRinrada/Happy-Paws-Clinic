using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Patients.GetWeightHistory;

/// <summary>
/// <c>GET /staff/pets/{id}/weights</c> — "ชุดข้อมูลกราฟน้ำหนักย้อนหลัง". <c>From</c> and <c>To</c>
/// bound the readings included; null means the whole history.
/// </summary>
public sealed record GetWeightHistoryQuery(string PetId, DateTimeOffset? From = null, DateTimeOffset? To = null);

/// <summary>
/// A patient's weight over time.
/// <para>
/// Read from the vitals recorded in each visit, the same rows the patient card's "น้ำหนักล่าสุด"
/// comes from, so the last point on the graph and the number beside it are always the same.
/// There is no weights table to keep in step.
/// </para>
/// </summary>
public sealed class GetWeightHistoryHandler(IPetRepository pets, IPetTimelineReader timeline)
{
    /// <summary>A graph does not need more points than a screen has pixels; this is about thirty years of monthly visits.</summary>
    public const int MaxPoints = 400;

    public async Task<WeightSeriesDto> HandleAsync(
        GetWeightHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.From is { } from && query.To is { } to && from > to)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["from"] = ["from ต้องไม่อยู่หลัง to"],
            });
        }

        var pet = await pets.FindByIdAsync(query.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", query.PetId);

        var points = await timeline.WeightSeriesAsync(pet.Id, query.From, query.To, MaxPoints, cancellationToken);

        return new WeightSeriesDto(pet.Id, "kg", points);
    }
}
