using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Application.Prescriptions.ListDrugOptions;
using HappyPaws.Application.Prescriptions.ListPrescriptions;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The medication history, and the prescriber's drug picker. Writing a prescription is a
/// consultation route (<c>POST /staff/consultations/{id}/prescriptions</c>); dispensing one is in
/// <see cref="PrescriptionDispenseController"/>.
/// </summary>
[ApiController]
[Route("staff/prescriptions")]
public sealed class PrescriptionsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/prescriptions?pet_id=</c> — "ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน".
    /// <para>
    /// Each entry lists, in <c>duplicate_of</c>, the other prescriptions of the same drug whose
    /// courses overlap it. With <c>pet_id</c> the read is written to the access log.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="petId">One patient's history.</param>
    /// <param name="dispensed"><c>false</c> for what the pharmacy has yet to hand over.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Prescription.Read)]
    [ProducesResponseType<CursorPage<PrescriptionItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CursorPage<PrescriptionItemDto>> List(
        [FromServices] ListPrescriptionsHandler handler,
        [FromQuery(Name = "pet_id")] string? petId,
        [FromQuery] bool? dispensed,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListPrescriptionsQuery(petId, dispensed, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

    /// <summary>
    /// <c>GET /staff/prescriptions/drug-options?q=&amp;branch=&amp;limit=</c> — the medicines a prescriber can
    /// pick, with the selling price and the stock status at one branch. <b>Not in the API reference</b>
    /// (<c>docs/contract-extensions.md § 20</c>).
    /// <para>
    /// <c>rx:write</c>, the scope that writes a prescription, and not <c>inventory:read</c>: a vet
    /// needs to see which drugs are stocked, not the stock room's lots, costs and orders. The answer is
    /// <c>{ "branch_id": "...", "data": [...] }</c>.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="q">Part of the drug's name.</param>
    /// <param name="branch">The branch whose stock is shown; normally the case's.</param>
    /// <param name="limit">1 to 50; 20 when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("drug-options")]
    [RequireScope(Scopes.Prescription.Write)]
    [ProducesResponseType<DrugOptionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<DrugOptionsDto> DrugOptions(
        [FromServices] ListDrugOptionsHandler handler,
        [FromQuery] string? q,
        [FromQuery] string? branch,
        [FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ListDrugOptionsQuery(q, branch, limit), cancellationToken);
}
