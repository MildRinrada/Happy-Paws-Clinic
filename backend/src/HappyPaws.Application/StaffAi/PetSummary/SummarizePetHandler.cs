using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.StaffAi.Privacy;
using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.StaffAi.PetSummary;

/// <summary>The last summary written for a pet, and the fingerprint of the data it was written from.</summary>
public sealed record CachedPetSummary(
    string PetId,
    string Fingerprint,
    string Text,
    IReadOnlyList<PetSummaryCitationDto> Citations,
    DateTimeOffset GeneratedAt);

/// <summary>One row per pet: the newest summary replaces the one before.</summary>
public interface IPetSummaryCache
{
    Task<CachedPetSummary?> FindAsync(string petId, CancellationToken cancellationToken = default);

    Task SaveAsync(CachedPetSummary summary, CancellationToken cancellationToken = default);
}

/// <summary>
/// <c>POST /staff/ai/pets/{id}/summary</c>. <c>text</c> is the paragraph with its <c>[R3]</c>
/// markers, <c>citations</c> maps each marker to the record behind it, <c>cached</c> says whether
/// it was served from the cache (and so not counted).
/// </summary>
public sealed record PetSummaryDto(
    string PetId,
    string Text,
    IReadOnlyList<PetSummaryCitationDto> Citations,
    bool Cached,
    DateTimeOffset GeneratedAt);

/// <summary>
/// A one-paragraph briefing on a patient for the vet about to see them, with every fact linked to
/// the record it came from.
/// <para>
/// <b>Gate.</b> <c>record:read</c> and <c>ai:use</c> at the route, the same reach as the pet's
/// history (<c>GET /staff/pets/{id}/history</c>), which is also not branch-bound. Every call writes
/// <c>ai.pet_summary</c> to the access log, cached or not: the summary is the patient's file read
/// another way.
/// </para>
/// <para>
/// <b>Cache.</b> The data block is fingerprinted; when the stored summary was written from the
/// same fingerprint it is returned with <c>cached: true</c> and no use is counted. Anything that
/// changes the data (a new visit, a result, a medicine running out) writes a new one.
/// </para>
/// </summary>
public sealed class SummarizePetHandler(
    IStaffAiReader reader,
    IPetSummaryCache cache,
    StaffAiGateway gateway,
    GetClinicProfileHandler profile,
    ClinicDays days,
    IAuditTrail audit,
    IClock clock)
{
    public async Task<PetSummaryDto> HandleAsync(string petId, CancellationToken cancellationToken = default)
    {
        gateway.EnsureEnabled();

        var file = await reader.PetFileAsync(petId, days.Today, cancellationToken)
            ?? throw new NotFoundException("pet", petId);

        var pseudonymizer = new Pseudonymizer();
        var input = PetSummaryAssembler.Build(file, days.Today, pseudonymizer);

        var stored = await cache.FindAsync(file.Pet.Id, cancellationToken);
        PetSummaryDto result;

        if (stored is not null && stored.Fingerprint == input.Fingerprint)
        {
            result = new PetSummaryDto(file.Pet.Id, stored.Text, stored.Citations, Cached: true, stored.GeneratedAt);
        }
        else
        {
            var clinic = await profile.HandleAsync(cancellationToken);
            var answer = await gateway.CompleteTextAsync(
                StaffAiFeatures.PetSummary,
                PetSummaryPrompt.System(clinic.Name),
                StaffAiPromptRules.Fence(input.Data),
                cancellationToken);

            var (text, cited) = Citations.Validate(pseudonymizer.Restore(answer), input.Sources);
            var summary = new CachedPetSummary(file.Pet.Id, input.Fingerprint, text, cited, clock.UtcNow);

            await cache.SaveAsync(summary, cancellationToken);
            result = new PetSummaryDto(file.Pet.Id, text, cited, Cached: false, summary.GeneratedAt);
        }

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.AiPetSummary,
                AuditResources.Pet,
                file.Pet.Id,
                file.Pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["cached"] = result.Cached,
                    ["sources"] = input.Sources.Count,
                    ["cited"] = result.Citations.Count,
                }),
            cancellationToken);

        return result;
    }
}
