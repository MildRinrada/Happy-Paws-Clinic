using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Patients;

/// <summary>
/// The cross-aggregate reads about one patient: what it last weighed, and what has happened to it.
/// <para>
/// A separate port from <see cref="IPetRepository"/> because these are <b>projections</b>, not the
/// Pet aggregate. Both read tables the Patients module does not own — vitals belong to a
/// consultation, prescriptions to their own aggregate — and both exist to answer a question about
/// the patient rather than to load one. Putting them on the Pet repository would make it look as
/// though a Pet has vitals, which is exactly the model this codebase avoided when it kept weight on
/// the visit that measured it.
/// </para>
/// </summary>
public interface IPetTimelineReader
{
    /// <summary>
    /// "น้ำหนักล่าสุด" — the newest vitals reading carrying a weight, or null if never weighed.
    /// <para>
    /// Read from the vitals rows rather than cached on the patient, so it can never disagree with
    /// the weight graph that reads the same rows.
    /// </para>
    /// </summary>
    Task<WeightDto?> LatestWeightAsync(string petId, CancellationToken cancellationToken = default);

    /// <summary>
    /// "ชุดข้อมูลกราฟน้ำหนักย้อนหลัง" — every reading carrying a weight, oldest first. When there are
    /// more than <paramref name="maxPoints"/>, the most recent ones are kept.
    /// </summary>
    Task<IReadOnlyList<WeightPointDto>> WeightSeriesAsync(
        string petId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxPoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /staff/pets/{id}/history</c> — "ไทม์ไลน์รวมทุกเคส วัคซีน ยา แล็บ", newest first.
    /// </summary>
    Task<CursorPage<PetTimelineEntryDto>> TimelineAsync(
        string petId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}
