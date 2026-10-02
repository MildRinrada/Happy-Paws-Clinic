using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations;

/// <summary>Persistence port for the clinic's own treatment templates.</summary>
public interface ITreatmentTemplateRepository
{
    void Add(TreatmentTemplate template);

    /// <summary>
    /// Active templates by name. <paramref name="species"/> keeps templates for that species and
    /// those written for any; <paramref name="query"/> matches the name or description.
    /// </summary>
    Task<IReadOnlyList<TreatmentTemplate>> ListActiveAsync(
        string? species,
        string? query,
        CancellationToken cancellationToken = default);

    Task<TreatmentTemplate?> FindActiveAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>Every template name already used, for the seed command to skip.</summary>
    Task<IReadOnlySet<string>> NamesAsync(CancellationToken cancellationToken = default);
}
