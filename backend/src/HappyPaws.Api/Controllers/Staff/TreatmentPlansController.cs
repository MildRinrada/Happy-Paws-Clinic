using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Patients;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Patients.CreateTreatmentPlan;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Treatment plans: a course of visits for one patient, planned and booked together.</summary>
[ApiController]
[Route("staff/pets/{id}/treatment-plans")]
public sealed class TreatmentPlansController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/pets/{id}/treatment-plans</c> — "แผนหลายครั้ง · ผูกกับนัดและการเตือน".
    /// <para>
    /// Books every session as an appointment unless <c>book</c> is false, all or nothing, so it
    /// requires <c>Idempotency-Key</c> like every POST that creates appointments.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">The service, the first session, and how the rest follow.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Consult.Write)]
    [ProducesResponseType<TreatmentPlanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TreatmentPlanDto>> Create(
        string id,
        [FromBody] CreateTreatmentPlanRequest request,
        [FromServices] CreateTreatmentPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var plan = await handler.HandleAsync(
            new CreateTreatmentPlanCommand(
                id,
                request.ServiceId,
                request.FirstStartsAt,
                request.Title,
                request.Rrule,
                request.Sessions,
                request.IntervalDays,
                request.TemplateId,
                request.ConsultationId,
                request.StaffId,
                request.BranchId,
                request.Notes,
                request.Book,
                request.Type),
            cancellationToken);

        return Created($"/staff/pets/{id}/treatment-plans/{plan.Id}", plan);
    }
}
