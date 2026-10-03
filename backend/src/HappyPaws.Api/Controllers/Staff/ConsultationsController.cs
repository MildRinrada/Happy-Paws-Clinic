using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Consultations;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Consultations.GetConsultation;
using HappyPaws.Application.Consultations.OpenConsultation;
using HappyPaws.Application.Consultations.UpdateConsultation;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Opening a case and keeping its draft saved. The parts of the case — vitals, examination,
/// diagnoses, treatments, prescriptions and the close — live in
/// <see cref="ConsultationPartsController"/>, because they carry different scopes and would
/// otherwise turn one controller into the whole examination room.
/// </summary>
[ApiController]
[Route("staff/consultations")]
public sealed class ConsultationsController : ControllerBase
{
    /// <summary><c>POST /staff/consultations</c> — "เปิดเคสจากคิวหรือจากนัด".</summary>
    /// <param name="request">The appointment or the queue ticket the case opens from.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConsultationDto>> Open(
        [FromBody] OpenConsultationRequest request,
        [FromServices] OpenConsultationHandler handler,
        CancellationToken cancellationToken)
    {
        var consultation = await handler.HandleAsync(
            new OpenConsultationCommand(request.AppointmentId, request.QueueTicketId, request.ChiefComplaint),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(consultation.Version);

        return Created($"/staff/consultations/{consultation.Id}", consultation);
    }

    /// <summary>
    /// <c>GET /staff/consultations/{id}</c> — <b>not in the API reference</b>; see
    /// <c>docs/contract-extensions.md § 14</c>. The case as every write in this section already
    /// answers with it, so a reloaded page can show a draft left open.
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Consult.Read)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationDto>> Get(
        string id,
        [FromServices] GetConsultationHandler handler,
        CancellationToken cancellationToken)
    {
        var consultation = await handler.HandleAsync(new GetConsultationQuery(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(consultation.Version);

        return Ok(consultation);
    }

    /// <summary>
    /// <c>PATCH /staff/consultations/{id}</c> — "autosave ฉบับร่างทุก 10 วินาที".
    /// <para>
    /// <c>If-Match</c> is honoured when sent but not required: an autosave loop that had to carry a
    /// version would conflict with itself every few seconds.
    /// </para>
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The fields the vet has touched. Omitted fields are left alone.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<ConsultationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<ConsultationDto>> Update(
        string id,
        [FromBody] UpdateConsultationRequest request,
        [FromServices] UpdateConsultationHandler handler,
        CancellationToken cancellationToken)
    {
        var consultation = await handler.HandleAsync(
            new UpdateConsultationCommand(
                id,
                request.ChiefComplaint,
                request.Subjective,
                request.Objective,
                request.Assessment,
                request.Plan,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(consultation.Version);

        return Ok(consultation);
    }
}
