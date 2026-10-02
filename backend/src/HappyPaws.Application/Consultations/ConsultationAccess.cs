using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations;

/// <summary>
/// Loads a case and decides who may act on it.
/// <para>
/// The gate is the <b>branch</b>, not the author. A vet handing over at the end of a shift, a
/// second opinion called in mid-consultation, an assistant entering vitals for someone else's
/// case — all are ordinary, and an owner-only rule would break each of them. What stops a case
/// from being opened by the wrong person is the scope plus the branch they work at, and every read
/// of the resulting record is written to the access log regardless.
/// </para>
/// <para>
/// Customers never reach this. The portal sees clinical history through
/// <c>GET /portal/pets/{id}/records</c> — "เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น" — which is a
/// different, narrower projection.
/// </para>
/// </summary>
public sealed class ConsultationAccess(IConsultationRepository consultations, BranchGuard branchGuard)
{
    public async Task<Consultation> ForStaffAsync(string consultationId, CancellationToken cancellationToken = default)
    {
        var consultation = await consultations.FindByIdAsync(consultationId, cancellationToken)
            ?? throw new NotFoundException("consultation", consultationId);

        branchGuard.EnsureCanAccess(consultation.BranchId);

        return consultation;
    }
}
