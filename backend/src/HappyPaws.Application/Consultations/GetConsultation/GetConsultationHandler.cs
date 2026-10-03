using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Consultations.GetConsultation;

/// <summary>
/// <c>GET /staff/consultations/{id}</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 14</c>.
/// </summary>
public sealed record GetConsultationQuery(string ConsultationId);

/// <summary>
/// One case, as the consulting room shows it.
/// <para>
/// The contract's consultation section is eight writes and no read. A case's id is known only from
/// the response of whichever call last touched it, so a vet who reloads the page, or comes back
/// after lunch to a draft left open, has no way to see what is already in it. Every other endpoint
/// in the section answers with this same <see cref="ConsultationDto"/>; this returns it without
/// changing anything.
/// </para>
/// <para>
/// Gated like every write on the case: <c>consult:read</c> and the branch (see
/// <see cref="ConsultationAccess"/>). <b>Audited when the reader is not the case's vet</b>, which is
/// what <see cref="AuditActions.ConsultationViewed"/> was declared for. The author reading their own
/// draft is not logged, for the reason autosave is not: a row per reload of the vet's own work
/// would bury the reads the access log exists to show.
/// </para>
/// </summary>
public sealed class GetConsultationHandler(
    ConsultationAccess access,
    ICurrentUser currentUser,
    IAuditTrail audit)
{
    public async Task<ConsultationDto> HandleAsync(
        GetConsultationQuery query,
        CancellationToken cancellationToken = default)
    {
        var consultation = await access.ForStaffAsync(query.ConsultationId, cancellationToken);

        if (currentUser.RequireStaffId() != consultation.VetStaffId)
        {
            await audit.RecordImmediatelyAsync(
                new AuditEvent(
                    AuditActions.ConsultationViewed,
                    AuditResources.Consultation,
                    consultation.Id,
                    consultation.PetId,
                    consultation.BranchId),
                cancellationToken);
        }

        return ConsultationDto.From(consultation);
    }
}
