using HappyPaws.Application.Consultations;
using HappyPaws.Domain.Consultations;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Consultations;

internal sealed class TreatmentTemplateRepository(HappyPawsDbContext db) : ITreatmentTemplateRepository
{
    public void Add(TreatmentTemplate template) => db.Set<TreatmentTemplate>().Add(template);

    public async Task<IReadOnlyList<TreatmentTemplate>> ListActiveAsync(
        string? species,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var templates = db.Set<TreatmentTemplate>().AsNoTracking().Where(template => template.IsActive);

        if (species is not null)
        {
            templates = templates.Where(template => template.Species == null || template.Species == species);
        }

        if (query is not null)
        {
            var pattern = $"%{query}%";
            templates = templates.Where(template =>
                EF.Functions.ILike(template.Name, pattern)
                || (template.Description != null && EF.Functions.ILike(template.Description, pattern)));
        }

        return await templates.OrderBy(template => template.Name).ToListAsync(cancellationToken);
    }

    public Task<TreatmentTemplate?> FindActiveAsync(string templateId, CancellationToken cancellationToken = default) =>
        db.Set<TreatmentTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(template => template.Id == templateId && template.IsActive, cancellationToken);

    public async Task<IReadOnlySet<string>> NamesAsync(CancellationToken cancellationToken = default) =>
        (await db.Set<TreatmentTemplate>().Select(template => template.Name).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
}
