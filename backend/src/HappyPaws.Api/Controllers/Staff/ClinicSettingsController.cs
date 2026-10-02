using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Clinics;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.ManageBookingSettings;
using HappyPaws.Application.Clinics.UpdateMedicalRecordSettings;
using HappyPaws.Application.Clinics.UpdateQueueSettings;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The clinic's operating rules: booking, queue numbering, and what a medical record requires.
/// <para>
/// One read and three writes, which is what the contract gives. Only booking has a <c>GET</c>; the
/// other two are <c>PATCH</c>-only, so an empty <c>PATCH</c> body is how their current values are
/// read — it writes nothing and returns the settings as they stand. See
/// <c>docs/assumptions.md § 27</c>.
/// </para>
/// </summary>
[ApiController]
[Route("clinic/settings")]
public sealed class ClinicSettingsController : ControllerBase
{
    /// <summary><c>GET /clinic/settings/booking</c> — "ช่วงเวลาจอง lead time โควตา walk-in".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("booking")]
    [RequireScope(Scopes.Settings.Read)]
    [ProducesResponseType<BookingSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingSettingsDto>> GetBooking(
        [FromServices] GetBookingSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }

    /// <summary>
    /// <c>PATCH /clinic/settings/booking</c> — "slot 15/20/30 นาที · ยกเลิกล่วงหน้ากี่ชั่วโมง".
    /// </summary>
    /// <param name="request">The rules to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("booking")]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<BookingSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingSettingsDto>> UpdateBooking(
        [FromBody] UpdateBookingSettingsRequest request,
        [FromServices] UpdateBookingSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(
            new UpdateBookingSettingsCommand(
                request.SlotMinutes,
                request.LeadTimeMinutes,
                request.CancellationCutoffHours,
                request.MaxAdvanceDays,
                request.WalkInSlotsPerHour,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }

    /// <summary>
    /// <c>PATCH /clinic/settings/queue</c> — "รูปแบบเลขคิว การรีเซ็ตรายวัน". Applies to tickets
    /// issued from now on; a ticket already printed keeps its number.
    /// </summary>
    /// <param name="request">Prefix, digit count, and whether numbering restarts each day.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("queue")]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<QueueSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueSettingsDto>> UpdateQueue(
        [FromBody] UpdateQueueSettingsRequest request,
        [FromServices] UpdateQueueSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(
            new UpdateQueueSettingsCommand(
                request.NumberPrefix,
                request.NumberDigits,
                request.ResetDaily,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }

    /// <summary>
    /// <c>PATCH /clinic/settings/medical-record</c> — "ฟิลด์บังคับ แบบฟอร์มตรวจ การ sign".
    /// <para>
    /// Required fields are checked when a case is closed, the examination form when findings are
    /// recorded, and the signing rule when a record is signed. Nothing reaches back to records
    /// already signed.
    /// </para>
    /// </summary>
    /// <param name="request">The rules to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("medical-record")]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<MedicalRecordSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MedicalRecordSettingsDto>> UpdateMedicalRecord(
        [FromBody] UpdateMedicalRecordSettingsRequest request,
        [FromServices] UpdateMedicalRecordSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(
            new UpdateMedicalRecordSettingsCommand(
                request.RequiredFields,
                request.ExaminationSystems,
                request.OwnerVisibleSections,
                request.SignerMustBeCaseVet,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }
}
