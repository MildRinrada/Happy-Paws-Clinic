using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords;

/// <summary>
/// Persistence port for signed records. There is no <c>Delete</c> and there never will be —
/// "ลบไม่ได้ · ทุกฉบับเก็บถาวรพร้อมผู้ลงนาม".
/// </summary>
public interface IMedicalRecordRepository
{
    void Add(MedicalRecord record);

    /// <summary>Loads a record with its amendment history.</summary>
    Task<MedicalRecord?> FindByIdAsync(string recordId, CancellationToken cancellationToken = default);

    Task<MedicalRecord?> FindByConsultationAsync(string consultationId, CancellationToken cancellationToken = default);

    /// <summary>A patient's records with their amendments, newest first, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<MedicalRecord>> ForPetAsync(string petId, int take, CancellationToken cancellationToken = default);
}
