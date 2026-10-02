using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Patients;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients.CreatePet;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.GetPet;
using HappyPaws.Application.Patients.ListPets;
using HappyPaws.Application.Patients.MarkPetDeceased;
using HappyPaws.Application.Patients.RecordAllergy;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The patient register. Carer links are in <see cref="PetCarersController"/> and the clinical
/// timeline in <see cref="PetHistoryController"/>, because both carry different scopes — and the
/// timeline's is <c>record:read</c>, which is the line between an identity card and a medical file.
/// </summary>
[ApiController]
[Route("staff/pets")]
public sealed class PetsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/pets</c> — "ทะเบียนคนไข้ทั้งคลินิก".
    /// <para>
    /// Closed records are excluded unless <c>?include_deceased=true</c>. A register that grows
    /// forever with every patient who ever lived stops being usable at a counter.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="q">Name, hospital number or microchip.</param>
    /// <param name="customerId">One owner's patients.</param>
    /// <param name="species">Narrows to one species.</param>
    /// <param name="includeDeceased">Include closed records.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Patient.Read)]
    [ProducesResponseType<CursorPage<PetSummaryDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<PetSummaryDto>> List(
        [FromServices] ListPetsHandler handler,
        [FromQuery] string? q,
        [FromQuery(Name = "customer_id")] string? customerId,
        [FromQuery] string? species,
        [FromQuery(Name = "include_deceased")] bool includeDeceased,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListPetsQuery(
                q,
                customerId,
                species,
                includeDeceased,
                limit is 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/pets</c> — "ออก HN อัตโนมัติ · ผูกเจ้าของหลัก".
    /// <para>
    /// The owner is written twice, deliberately: as the patient's billing contact and as a carer
    /// link. They are different questions and they diverge — see <c>CreatePetHandler</c>.
    /// </para>
    /// </summary>
    /// <param name="request">Name, species, primary owner, and optionally breed and chip.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Patient.Write)]
    [ProducesResponseType<PetSummaryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PetSummaryDto>> Create(
        [FromBody] CreatePetRequest request,
        [FromServices] CreatePetHandler handler,
        CancellationToken cancellationToken)
    {
        var pet = await handler.HandleAsync(
            new CreatePetCommand(
                request.Name,
                request.Species,
                request.PrimaryCustomerId,
                request.Breed,
                request.MicrochipNumber),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(pet.Version);

        return Created($"/staff/pets/{pet.Id}", pet);
    }

    /// <summary>
    /// <c>GET /staff/pets/{id}</c> — "สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป".
    /// <para>
    /// Allergies ship with the patient rather than behind a second call: they are specified as a red
    /// banner on every relevant screen, and a banner that loads separately is one that sometimes
    /// loads late.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Patient.Read)]
    [ProducesResponseType<PetDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetDetailDto>> Get(
        string id,
        [FromServices] GetPetHandler handler,
        CancellationToken cancellationToken)
    {
        var detail = await handler.HandleAsync(new GetPetQuery(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(detail.Pet.Version);

        return Ok(detail);
    }

    /// <summary>
    /// <c>POST /staff/pets/{id}/allergies</c> —
    /// "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง".
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">The substance, how badly, and what happened.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/allergies")]
    [RequireScope(Scopes.Record.Write)]
    [ProducesResponseType<AllergyDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllergyDto>> Allergies(
        string id,
        [FromBody] RecordAllergyRequest request,
        [FromServices] RecordAllergyHandler handler,
        CancellationToken cancellationToken)
    {
        var allergy = await handler.HandleAsync(
            new RecordAllergyCommand(id, request.Substance, request.Severity, request.Reaction),
            cancellationToken);

        return Created($"/staff/pets/{id}/allergies/{allergy.Id}", allergy);
    }

    /// <summary>
    /// <c>POST /staff/pets/{id}/deceased</c> —
    /// "ปิดเรกคอร์ดอย่างสุภาพ · หยุดแจ้งเตือนทุกชนิด".
    /// <para>
    /// Future visits already in the calendar are <b>cancelled</b>, and the owner is told once. The
    /// contract does not say what should happen to them, and its silence leaves the worst default in
    /// place — a reminder, or a call from the counter, about a pet that has died. See
    /// <c>docs/contract-extensions.md § 6</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">When it happened, and any note for the record.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/deceased")]
    [RequireScope(Scopes.Patient.Write)]
    [ProducesResponseType<PetDeceasedResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PetDeceasedResult> Deceased(
        string id,
        [FromBody] MarkDeceasedRequest request,
        [FromServices] MarkPetDeceasedHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new MarkPetDeceasedCommand(id, request.OccurredAt, request.Note), cancellationToken);
}
