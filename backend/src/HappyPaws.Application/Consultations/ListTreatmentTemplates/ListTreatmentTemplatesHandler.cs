using HappyPaws.Application.Consultations.Dtos;

namespace HappyPaws.Application.Consultations.ListTreatmentTemplates;

/// <summary><c>GET /staff/templates/treatments</c> — "เทมเพลตแผนรักษาที่คลินิกทำไว้เอง".</summary>
/// <param name="Species">Templates for this species, plus those written for any species.</param>
/// <param name="Query">Part of a name or description.</param>
public sealed record ListTreatmentTemplatesQuery(string? Species = null, string? Query = null);

/// <summary>
/// The clinic's templates, for the exam room to pick from.
/// <para>
/// Not paged: a clinic keeps tens of these, not thousands, and the picker wants them all. The
/// contract gives no way to write a template, so they come from the operator's
/// <c>seed-treatment-templates</c> command. See <c>docs/assumptions.md § 36</c>.
/// </para>
/// </summary>
public sealed class ListTreatmentTemplatesHandler(ITreatmentTemplateRepository templates)
{
    public async Task<IReadOnlyList<TreatmentTemplateDto>> HandleAsync(
        ListTreatmentTemplatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var found = await templates.ListActiveAsync(
            string.IsNullOrWhiteSpace(query.Species) ? null : query.Species.Trim(),
            string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            cancellationToken);

        return [.. found.Select(TreatmentTemplateDto.From)];
    }
}
