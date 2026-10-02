using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Customers;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Customers.CreateCustomer;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Customers.GetCustomer;
using HappyPaws.Application.Customers.InviteToPortal;
using HappyPaws.Application.Customers.MergeCustomers;
using HappyPaws.Application.Customers.SearchCustomers;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The customer register — "เจ้าของหนึ่งคนมีหลายสัตว์ · สัตว์หนึ่งตัวมีผู้ดูแลได้หลายคน".
/// <para>
/// Three scopes across five routes, and the third one is the interesting one:
/// <c>customer:merge</c> exists separately from <c>customer:write</c> because merging is the only
/// operation here that moves clinical history between records and cannot be undone through the API.
/// A receptionist creates and invites; folding two people into one is a different permission.
/// </para>
/// </summary>
[ApiController]
[Route("staff/customers")]
public sealed class CustomersController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/customers?q=</c> — "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์".
    /// <para>
    /// One box, four things behind it, because that is what the counter is actually told: a name, a
    /// number, or "I'm here with ลูน่า". Merged records never appear.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="q">Name, phone, hospital number or a patient's name.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Customer.Read)]
    [ProducesResponseType<CursorPage<CustomerDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<CustomerDto>> Search(
        [FromServices] SearchCustomersHandler handler,
        [FromQuery] string? q,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new SearchCustomersQuery(q, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/customers</c> — "สร้างจากเคาน์เตอร์ · ตรวจซ้ำด้วยเบอร์โทร".
    /// <para>
    /// A phone number already on file answers <c>409 duplicate_customer</c> and lists the matching
    /// records; resend with <c>allow_duplicate_phone</c> to create a second anyway. A household
    /// really does share one number.
    /// </para>
    /// </summary>
    /// <param name="request">Name, phone, and optionally an email.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Customer.Write)]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        [FromServices] CreateCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var customer = await handler.HandleAsync(
            new CreateCustomerCommand(
                request.DisplayName, request.Phone, request.Email, request.AllowDuplicatePhone),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(customer.Version);

        return Created($"/staff/customers/{customer.Id}", customer);
    }

    /// <summary>
    /// <c>GET /staff/customers/{id}</c> — "ข้อมูลติดต่อ สัตว์ทั้งหมด ยอดค้างชำระ".
    /// <para>
    /// A merged record still resolves and carries <c>merged_into_customer_id</c>, so an id from an
    /// old receipt says where it went instead of answering 404.
    /// </para>
    /// </summary>
    /// <param name="id">Customer id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Customer.Read)]
    [ProducesResponseType<CustomerDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailDto>> Get(
        string id,
        [FromServices] GetCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var detail = await handler.HandleAsync(new GetCustomerQuery(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(detail.Customer.Version);

        return Ok(detail);
    }

    /// <summary>
    /// <c>POST /staff/customers/{id}/invite</c> — "ชวนให้เปิดใช้ portal และผูก LINE".
    /// <para>
    /// The portal half runs; re-inviting refreshes the live invitation rather than stacking rows.
    /// The LINE half does not, by a standing decision — LINE is an outbound channel here, never an
    /// identity. See <c>docs/assumptions.md § A</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Customer id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/invite")]
    [RequireScope(Scopes.Customer.Write)]
    [ProducesResponseType<InvitationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<InvitationDto> Invite(
        string id,
        [FromServices] InviteToPortalHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new InviteToPortalCommand(id), cancellationToken);

    /// <summary>
    /// <c>POST /staff/customers/{id}/merge</c> — "รวมเรกคอร์ดซ้ำ · ย้ายสัตว์และประวัติทั้งหมด".
    /// <para>
    /// <b>The path id survives</b> and the body names the duplicate it absorbs. Not reversible
    /// through the API, so it carries its own scope and the whole move goes into the audit log with
    /// a row count per table. See <c>docs/assumptions.md § 12</c>.
    /// </para>
    /// </summary>
    /// <param name="id">The customer record being kept.</param>
    /// <param name="request">The duplicate to fold into it.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/merge")]
    [RequireScope(Scopes.Customer.Merge)]
    [ProducesResponseType<MergeResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<MergeResultDto> Merge(
        string id,
        [FromBody] MergeCustomerRequest request,
        [FromServices] MergeCustomersHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new MergeCustomersCommand(
                id, request.DuplicateCustomerId, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);
}
