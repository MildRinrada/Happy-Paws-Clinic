using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Consultations.ListTreatmentTemplates;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The clinic's treatment templates. Read-only: the contract gives no way to write one, so they
/// come from the operator's <c>seed-treatment-templates</c> command (<c>docs/assumptions.md § 36</c>).
/// </summary>
[ApiController]
[Route("staff/templates/treatments")]
public sealed class TreatmentTemplatesController : ControllerBase
{
    /// <summary><c>GET /staff/templates/treatments</c> — "เทมเพลตแผนรักษาที่คลินิกทำไว้เอง".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="species">Templates for this species, plus those for any species.</param>
    /// <param name="q">Part of a name or description.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Consult.Read)]
    [ProducesResponseType<ListResponse<TreatmentTemplateDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<TreatmentTemplateDto>> List(
        [FromServices] ListTreatmentTemplatesHandler handler,
        [FromQuery] string? species,
        [FromQuery] string? q,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(new ListTreatmentTemplatesQuery(species, q), cancellationToken));
}
