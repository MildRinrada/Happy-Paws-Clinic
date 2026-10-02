using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Consultations;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Consultations.AddDiagnosis;
using HappyPaws.Application.Consultations.AddPrescription;
using HappyPaws.Application.Consultations.AddTreatment;
using HappyPaws.Application.Consultations.CloseConsultation;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Consultations.RecordExamination;
using HappyPaws.Application.Consultations.RecordVitals;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The parts of a case, and the close that signs it.
/// <para>
/// Read the scopes down the file: <c>vitals:write</c> lets an assistant weigh a patient and nothing
/// else, <c>consult:write</c> is the vet's working surface, <c>rx:write</c> is prescribing, and
/// <c>record:sign</c> — held by neither an assistant nor a receptionist — is what puts a signature
/// on a permanent clinical document. That gradient is the contract's, and it is why these six
/// routes are not one endpoint taking a payload.
/// </para>
/// <para>
/// Every action answers <c>423 record_locked</c> once the case is closed: a signed record is
/// corrected through <c>POST /staff/medical-records/{id}/amendments</c>, never in place.
/// </para>
/// </summary>
[ApiController]
[Route("staff/consultations/{id}")]
public sealed class ConsultationPartsController : ControllerBase
{
    /// <summary><c>POST /staff/consultations/{id}/vitals</c> — "ผู้ช่วยกรอกได้ · น้ำหนักเข้ากราฟอัตโนมัติ".</summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">Weight, temperature, pulse, respiration.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("vitals")]
    [RequireScope(Scopes.Vitals.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Vitals(
        string id,
        [FromBody] RecordVitalsRequest request,
        [FromServices] RecordVitalsHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new RecordVitalsCommand(
                id,
                request.WeightKg,
                request.TemperatureC,
                request.HeartRateBpm,
                request.RespiratoryRateBpm,
                request.Note),
            cancellationToken));

    /// <summary><c>POST /staff/consultations/{id}/examination</c> — "ผลตรวจรายระบบ ปกติ/ผิดปกติ + บันทึก".</summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The whole examination form; re-posting replaces it.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("examination")]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Examination(
        string id,
        [FromBody] RecordExaminationRequest request,
        [FromServices] RecordExaminationHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new RecordExaminationCommand(id, request.Findings), cancellationToken));

    /// <summary><c>POST /staff/consultations/{id}/diagnoses</c> — "วินิจฉัยหลัก/รอง · ผูกรหัสโรคของคลินิก".</summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The diagnosis, and whether it is the principal one.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("diagnoses")]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Diagnoses(
        string id,
        [FromBody] AddDiagnosisRequest request,
        [FromServices] AddDiagnosisHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new AddDiagnosisCommand(id, request.Description, request.IsPrimary, request.Code, request.Note),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/consultations/{id}/treatments</c> — "หัตถการที่ทำในคลินิก · เข้าบิลอัตโนมัติ".
    /// Idempotent because a repeated line is a repeated charge.
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">What was done, how many, at what price.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("treatments")]
    [Idempotent]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Treatments(
        string id,
        [FromBody] AddTreatmentRequest request,
        [FromServices] AddTreatmentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new AddTreatmentCommand(
                id, request.Name, request.Quantity, request.UnitPriceSatang, request.ServiceId, request.Note),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/consultations/{id}/prescriptions</c> — "ตรวจแพ้ยาและตัดสต๊อกเมื่อจ่ายจริง".
    /// <para>
    /// Answers <c>409 allergy_conflict</c> when the patient has a recorded allergy to the drug,
    /// naming it in <c>details</c>. Resending with <c>acknowledge_allergy</c> proceeds and is
    /// audited; a severe allergy cannot be overridden here at all.
    /// </para>
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The drug, dose, frequency and quantity.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("prescriptions")]
    [Idempotent]
    [RequireScope(Scopes.Prescription.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Prescriptions(
        string id,
        [FromBody] AddPrescriptionRequest request,
        [FromServices] AddPrescriptionHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new AddPrescriptionCommand(
                id,
                request.DrugName,
                request.Dose,
                request.Frequency,
                request.DurationDays,
                request.Quantity,
                request.Instructions,
                request.AcknowledgeAllergy),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/consultations/{id}/close</c> —
    /// "sign เวชระเบียน ออกบิล สร้างนัดติดตาม ส่งสรุป".
    /// <para>
    /// <c>record:sign</c>, not <c>consult:write</c>. Writing on a case and putting a signature on
    /// the permanent record are different powers, and the contract's default roles hand them out
    /// differently.
    /// </para>
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The optional follow-up visit to book on the way out.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("close")]
    [Idempotent]
    [RequireScope(Scopes.Record.Sign)]
    [ProducesResponseType<CloseConsultationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<CloseConsultationResult>> Close(
        string id,
        [FromBody] CloseConsultationRequest request,
        [FromServices] CloseConsultationHandler handler,
        CancellationToken cancellationToken)
    {
        var followUp = request.FollowUp is null
            ? null
            : new FollowUpRequest(request.FollowUp.StartsAt, request.FollowUp.ServiceId, request.FollowUp.StaffId);

        var result = await handler.HandleAsync(
            new CloseConsultationCommand(id, followUp, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(result.Consultation.Version);

        return Ok(result);
    }

    private ActionResult<ConsultationDto> WithETag(ConsultationDto consultation)
    {
        Response.Headers.ETag = IfMatch.ToETag(consultation.Version);
        return Ok(consultation);
    }
}
