using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Laboratory;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords.GetOwnerVisibleRecords;

/// <summary><c>GET /portal/pets/{id}/records</c> — "เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น".</summary>
public sealed record GetOwnerVisibleRecordsQuery(string PetId);

/// <summary>
/// A pet's history as its owner may see it.
/// <para>
/// <b>Projected, not filtered.</b> The answer is built section by section from the signed record
/// into owner-shaped types that have no field for anything the clinic keeps to itself: no prices,
/// no staff notes, no ids of who did what. A section the clinic has not opened
/// (<c>PATCH /clinic/settings/medical-record</c>, <c>owner_visible_sections</c>) is not computed at
/// all. With the default settings nothing clinical is open, and the owner sees the dates of visits
/// and nothing else, which is the safe place to start. See <c>docs/assumptions.md § 10</c>.
/// </para>
/// <para>
/// Only a carer whose link allows history gets past the door; anyone else is <c>404</c>. Lab
/// results appear only after a vet has read them: an owner should not learn of an abnormal value
/// from a web page before anyone at the clinic has.
/// </para>
/// </summary>
public sealed class GetOwnerVisibleRecordsHandler(
    PetAccessGuard petAccess,
    IClinicSettingsStore settings,
    IMedicalRecordRepository records,
    ILabRequestRepository labs,
    IPetDocumentRepository documents,
    IFileStorage storage)
{
    /// <summary>A lifetime of visits for most pets; the owner's page is not an archive export.</summary>
    public const int MaxVisits = 100;

    public async Task<OwnerRecordsDto> HandleAsync(GetOwnerVisibleRecordsQuery query, CancellationToken cancellationToken = default)
    {
        await petAccess.EnsureMayViewHistoryAsync(query.PetId, cancellationToken);

        var visible = (await settings.MedicalRecordAsync(cancellationToken)).OwnerVisibleSections.ToHashSet(StringComparer.Ordinal);
        var signed = await records.ForPetAsync(query.PetId, MaxVisits, cancellationToken);

        return new OwnerRecordsDto(
            query.PetId,
            [.. visible.Order(StringComparer.Ordinal)],
            [.. signed.Select(record => Visit(record, visible))],
            visible.Contains("lab_result") ? await LabResultsAsync(query.PetId, cancellationToken) : null,
            visible.Contains("attachments") ? await AttachmentsAsync(query.PetId, cancellationToken) : null);
    }

    private static OwnerVisitDto Visit(MedicalRecord record, IReadOnlySet<string> visible)
    {
        var content = RecordContent.Read(record.CurrentContentJson);

        if (content is null)
        {
            return new OwnerVisitDto(record.Id, record.SignedAt, record.SignedAt);
        }

        return new OwnerVisitDto(
            record.Id,
            content.OpenedAt,
            record.SignedAt,
            visible.Contains("vitals")
                ? [.. content.Vitals.Select(v => new OwnerVitalsDto(v.RecordedAt, v.WeightKg, v.TemperatureC, v.HeartRateBpm, v.RespiratoryRateBpm))]
                : null,
            visible.Contains("examination")
                ? [.. content.Examination.Select(f => new OwnerFindingDto(f.BodySystem, f.IsNormal, f.Note))]
                : null,
            visible.Contains("diagnosis") ? [.. content.Diagnoses.Select(d => d.Description)] : null,
            visible.Contains("treatment") ? [.. content.Treatments.Select(t => new OwnerTreatmentDto(t.Name, t.Quantity))] : null,
            visible.Contains("prescription") ? [.. content.Prescriptions.Select(Prescription)] : null);
    }

    private static OwnerPrescriptionDto Prescription(PrescriptionDto p) =>
        new(p.DrugName, p.Dose, p.Frequency, p.DurationDays, p.Instructions);

    private async Task<IReadOnlyList<OwnerLabResultDto>> LabResultsAsync(string petId, CancellationToken cancellationToken)
    {
        var page = await labs.ListAsync(
            new LabRequestFilter(Status: LabRequestStatus.Reviewed, PetId: petId),
            new CursorPageRequest(CursorPaging.MaxLimit),
            cancellationToken);

        return
        [
            .. page.Data.Select(request => new OwnerLabResultDto(
                request.Id,
                request.RequestedAt,
                request.ReviewedAt,
                [.. request.Tests.Select(test => test.Name)],
                // A voided value (docs/contract-extensions.md § 52) was never the pet's result.
                [.. request.Results.Where(value => !value.IsVoided).Select(value => new OwnerLabValueDto(
                    value.Analyte,
                    value.ValueNumeric,
                    value.ValueText,
                    value.Unit,
                    value.Flag is { } flag ? WireEnum.Of(flag) : null))])),
        ];
    }

    private async Task<IReadOnlyList<OwnerAttachmentDto>> AttachmentsAsync(string petId, CancellationToken cancellationToken)
    {
        var files = await documents.ForPetAsync(petId, ownerVisibleOnly: true, cancellationToken);

        return
        [
            .. files.Select(document => new OwnerAttachmentDto(
                document.Id,
                document.Title,
                document.Kind,
                WireEnum.Of(document.Source),
                document.ContentType,
                document.CreatedAt,
                storage.CreateDownloadUrl(document.StorageKey, PetDocumentDto.DownloadLinkLifetime))),
        ];
    }
}
