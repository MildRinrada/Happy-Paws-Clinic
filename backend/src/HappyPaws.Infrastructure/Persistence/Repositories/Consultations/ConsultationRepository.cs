using HappyPaws.Application.Consultations;
using HappyPaws.Domain.Consultations;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Consultations;

/// <summary>
/// EF Core implementation of the consultation port.
/// <para>
/// Every load pulls all five child collections, and does so as a <b>split query</b>. Joining five
/// one-to-many collections in a single statement multiplies the rows together — three vitals, four
/// findings and two diagnoses become twenty-four rows carrying the same case over and over. Split
/// queries trade one round trip per collection for a result set that stays the size of the data.
/// </para>
/// </summary>
internal sealed class ConsultationRepository(HappyPawsDbContext db) : IConsultationRepository
{
    public void Add(Consultation consultation) => db.Set<Consultation>().Add(consultation);

    public Task<Consultation?> FindByIdAsync(
        string consultationId,
        CancellationToken cancellationToken = default) =>
        WithParts(db.Set<Consultation>())
            .FirstOrDefaultAsync(consultation => consultation.Id == consultationId, cancellationToken);

    public Task<Consultation?> FindByOriginAsync(
        string? appointmentId,
        string? queueTicketId,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId is not { Length: > 0 } && queueTicketId is not { Length: > 0 })
        {
            return Task.FromResult<Consultation?>(null);
        }

        return WithParts(db.Set<Consultation>())
            .FirstOrDefaultAsync(
                consultation =>
                    (appointmentId != null && consultation.AppointmentId == appointmentId)
                    || (queueTicketId != null && consultation.QueueTicketId == queueTicketId),
                cancellationToken);
    }

    private static IQueryable<Consultation> WithParts(IQueryable<Consultation> query) =>
        query
            .Include(consultation => consultation.Vitals)
            .Include(consultation => consultation.Findings)
            .Include(consultation => consultation.Diagnoses)
            .Include(consultation => consultation.Treatments)
            .Include(consultation => consultation.Prescriptions)
            .AsSplitQuery();
}
