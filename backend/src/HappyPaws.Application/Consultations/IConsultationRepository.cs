using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations;

/// <summary>
/// Persistence port for the consultation aggregate.
/// <para>
/// <see cref="FindByIdAsync"/> returns the case <b>with all five child collections loaded</b>.
/// Partial loading is not offered on purpose: every write goes through the aggregate root, and a
/// root holding an empty <c>Diagnoses</c> list because nobody included it would let
/// <c>Close</c> refuse a case that actually has a diagnosis — or, worse, let
/// <c>ReplaceExamination</c> clear findings it never read.
/// </para>
/// </summary>
public interface IConsultationRepository
{
    void Add(Consultation consultation);

    Task<Consultation?> FindByIdAsync(string consultationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The case already open for an appointment or a queue ticket, if any.
    /// <para>
    /// Opening a case is idempotent through this: a vet whose first request timed out presses again
    /// and is handed the same <c>con_…</c> rather than starting a parallel record of one visit.
    /// </para>
    /// </summary>
    Task<Consultation?> FindByOriginAsync(
        string? appointmentId,
        string? queueTicketId,
        CancellationToken cancellationToken = default);
}
