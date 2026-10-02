using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.UpdateConsultation;

/// <summary>
/// <c>PATCH /staff/consultations/{id}</c> — "autosave ฉบับร่างทุก 10 วินาที".
/// <para>
/// Every field is nullable and null means "not sent". An autosave posts what the vet has touched,
/// so a handler that treated absence as "clear it" would wipe the examination notes each time the
/// complaint box changed.
/// </para>
/// </summary>
public sealed record UpdateConsultationCommand(
    string ConsultationId,
    string? ChiefComplaint = null,
    string? Subjective = null,
    string? Objective = null,
    string? Assessment = null,
    string? Plan = null,
    uint? IfMatchVersion = null);

/// <summary>
/// Saves the working draft.
/// <para>
/// This runs every ten seconds per open case, which shapes two decisions. It is not audited — a
/// row per autosave would bury every meaningful entry in the log, and the signed record plus its
/// amendments already capture what the case ended up saying. And <c>If-Match</c> is honoured but
/// not required: an autosave loop that had to carry a version would fight itself, while a client
/// that does send one still gets the conflict detection it asked for.
/// </para>
/// </summary>
public sealed class UpdateConsultationHandler(
    ConsultationAccess access,
    IUnitOfWork unitOfWork)
{
    public async Task<ConsultationDto> HandleAsync(
        UpdateConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            ConcurrencyGuard.Ensure(consultation, command.IfMatchVersion);

            consultation.UpdateDraft(
                command.ChiefComplaint,
                command.Subjective,
                command.Objective,
                command.Assessment,
                command.Plan);

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }
}
