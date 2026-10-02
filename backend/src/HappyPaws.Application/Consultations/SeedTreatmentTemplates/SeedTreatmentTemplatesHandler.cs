using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Consultations.SeedTreatmentTemplates;

/// <summary>What <c>seed-treatment-templates</c> did.</summary>
/// <param name="Created">Template names added by this run.</param>
/// <param name="Skipped">Names already in use, left as they were.</param>
public sealed record SeedTreatmentTemplatesResult(IReadOnlyList<string> Created, IReadOnlyList<string> Skipped);

/// <summary>
/// Adds the templates in <see cref="SampleTreatmentTemplates"/>. Run by the operator's
/// <c>seed-treatment-templates</c> command, never by a request.
/// <para>
/// The contract describes these templates as the clinic's own ("ที่คลินิกทำไว้เอง") but gives no
/// endpoint to write one, so for now this command is the only way in. Safe to run again: a name
/// that exists is skipped.
/// </para>
/// </summary>
public sealed class SeedTreatmentTemplatesHandler(ITreatmentTemplateRepository templates, IUnitOfWork unitOfWork)
{
    public async Task<SeedTreatmentTemplatesResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var skipped = new List<string>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            created.Clear();
            skipped.Clear();

            var taken = await templates.NamesAsync(token);

            foreach (var template in SampleTreatmentTemplates.Create())
            {
                if (taken.Contains(template.Name))
                {
                    skipped.Add(template.Name);
                    continue;
                }

                templates.Add(template);
                created.Add(template.Name);
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return new SeedTreatmentTemplatesResult(created, skipped);
    }
}
