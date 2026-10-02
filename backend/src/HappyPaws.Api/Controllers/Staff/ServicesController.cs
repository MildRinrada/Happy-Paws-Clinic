using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.ServiceCatalog;
using HappyPaws.Api.Http;
using HappyPaws.Application.ServiceCatalog.CreateService;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Application.ServiceCatalog.UpdateAvailability;
using HappyPaws.Application.ServiceCatalog.UpdatePricing;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Managing the service catalogue. Three writes, all on <c>service:manage</c>.
/// <para>
/// The contract splits a service's price and its availability into endpoints of their own, and the
/// split is kept: what a visit costs and whether the public can book it are changed by different
/// people at different times, and each has its own rules about dates.
/// </para>
/// </summary>
[ApiController]
[Route("staff/services")]
[RequireScope(Scopes.Service.Manage)]
public sealed class ServicesController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/services</c> — "ชื่อ ราคา ระยะเวลา ผู้ให้บริการที่ทำได้".
    /// <para>
    /// A new service can be booked at the counter at once. It goes on the public menu only when
    /// <c>online_booking_enabled</c> is set, here or through the availability endpoint.
    /// </para>
    /// </summary>
    /// <param name="request">Name, category, duration, price, and optionally who may perform it.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromBody] CreateServiceRequest request,
        [FromServices] CreateServiceHandler handler,
        CancellationToken cancellationToken)
    {
        var service = await handler.HandleAsync(
            new CreateServiceCommand(
                request.Name,
                request.CategoryCode,
                request.DurationMinutes,
                request.PriceSatang,
                request.Code,
                request.EligibleStaffIds,
                request.OnlineBookingEnabled),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(service.Version);

        return Created("/services", service);
    }

    /// <summary>
    /// <c>PATCH /staff/services/{id}/pricing</c> — "ราคาตามน้ำหนัก/ชนิดสัตว์ · ราคามีผลวันที่".
    /// <para>
    /// Adds a dated price list. The current one stays in force until the new one's day arrives;
    /// re-sending a date replaces that day's list. A date in the past is refused.
    /// </para>
    /// </summary>
    /// <param name="id">Service id.</param>
    /// <param name="request">Base price, tiers, and the day they take effect.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}/pricing")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> UpdatePricing(
        string id,
        [FromBody] UpdatePricingRequest request,
        [FromServices] UpdatePricingHandler handler,
        CancellationToken cancellationToken)
    {
        var service = await handler.HandleAsync(
            new UpdatePricingCommand(
                id,
                request.BasePriceSatang,
                request.Tiers is null
                    ? null
                    : [.. request.Tiers.Select(tier =>
                        new PriceTierInput(tier.PriceSatang, tier.Species, tier.MinWeightKg, tier.MaxWeightKg))],
                request.EffectiveFrom,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(service.Version);

        return Ok(service);
    }

    /// <summary>
    /// <c>PATCH /staff/services/{id}/availability</c> — "เปิด/ปิดจองออนไลน์ · จำกัดสาขาหรือช่วงเวลา".
    /// <para>
    /// The online switch and the time windows limit what a customer can book for themselves; the
    /// branch list limits where the service is offered at all. Existing appointments are untouched.
    /// </para>
    /// </summary>
    /// <param name="id">Service id.</param>
    /// <param name="request">The switch, the branches, and the weekly windows.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}/availability")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> UpdateAvailability(
        string id,
        [FromBody] UpdateAvailabilityRequest request,
        [FromServices] UpdateAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var service = await handler.HandleAsync(
            new UpdateAvailabilityCommand(
                id,
                request.OnlineBookingEnabled,
                request.BranchIds,
                request.BookingWindows is null
                    ? null
                    : [.. request.BookingWindows.Select(window =>
                        new BookingWindowInput(window.Day, window.From, window.To))],
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(service.Version);

        return Ok(service);
    }
}
