using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Clinics;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.AddHoliday;
using HappyPaws.Application.Clinics.CreateBranch;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.BrandImages;
using HappyPaws.Application.Clinics.ReplaceOpeningHours;
using HappyPaws.Application.Clinics.UpdateBranch;
using HappyPaws.Application.Clinics.UpdateClinicProfile;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Clinics;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The writes behind the public clinic pages — profile, branches, hours and holidays.
/// <para>
/// Four routes, one scope. The contract puts every one of them on <c>settings:write</c> ("แก้ได้
/// เฉพาะเจ้าของและแอดมิน"), and the reads they pair with are public and live in
/// <c>Public/ClinicController</c>. The logo and tab icon uploads of § 50 are changes to the same
/// profile, so they sit here on the same scope.
/// </para>
/// </summary>
[ApiController]
[Route("clinic")]
[RequireScope(Scopes.Settings.Write)]
public sealed class ClinicSetupController : ControllerBase
{
    /// <summary>
    /// <c>PATCH /clinic</c> — "โปรไฟล์และ branding ของคลินิก". An absent field is left alone; an
    /// empty string clears it.
    /// </summary>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch]
    [ProducesResponseType<ClinicProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicProfileDto>> UpdateProfile(
        [FromBody] UpdateClinicProfileRequest request,
        [FromServices] UpdateClinicProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var profile = await handler.HandleAsync(
            new UpdateClinicProfileCommand(
                request.Name,
                request.LogoUrl,
                request.BrandColor,
                request.LicenseNo,
                request.OperatorName,
                IfMatch.Parse(Request.Headers.IfMatch),
                request.TabIconUrl,
                request.ShowNameWithLogo),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(profile.Version);

        return Ok(profile);
    }

    /// <summary>
    /// <c>POST /clinic/logo</c> — a signed URL to <c>PUT</c> a new logo to (PNG, JPEG or WebP, any
    /// shape, at most 5 MB, no SVG), and the <c>logo_url</c> value that names it. The profile does
    /// not change until a <c>PATCH /clinic</c> sends that value, after the file is in.
    /// <b>Contract extension</b>, see <c>docs/contract-extensions.md § 50</c>.
    /// </summary>
    /// <param name="request">The file's type and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("logo")]
    [ProducesResponseType<BrandImageUploadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<BrandImageUploadDto>> RequestLogo(
        [FromBody] BrandImageRequest request,
        [FromServices] RequestBrandImageHandler handler,
        CancellationToken cancellationToken) =>
        RequestBrandImageAsync(ClinicBrandImageKind.Logo, request, handler, cancellationToken);

    /// <summary>
    /// <c>POST /clinic/tab-icon</c> — the same for the optional square icon on the browser tab; the
    /// value goes in <c>tab_icon_url</c>. <b>Contract extension</b>, see
    /// <c>docs/contract-extensions.md § 50</c>.
    /// </summary>
    /// <param name="request">The file's type and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("tab-icon")]
    [ProducesResponseType<BrandImageUploadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<BrandImageUploadDto>> RequestTabIcon(
        [FromBody] BrandImageRequest request,
        [FromServices] RequestBrandImageHandler handler,
        CancellationToken cancellationToken) =>
        RequestBrandImageAsync(ClinicBrandImageKind.TabIcon, request, handler, cancellationToken);

    private async Task<ActionResult<BrandImageUploadDto>> RequestBrandImageAsync(
        ClinicBrandImageKind kind,
        BrandImageRequest request,
        RequestBrandImageHandler handler,
        CancellationToken cancellationToken)
    {
        var started = await handler.HandleAsync(
            new RequestBrandImageCommand(kind, request.ContentType, request.SizeBytes), cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(started);
    }

    /// <summary>
    /// <c>POST /clinic/branches</c> — "เปิดสาขาใหม่ในคลินิกเดียวกัน". The code becomes the id:
    /// <c>sukhumvit39</c> → <c>brn_sukhumvit39</c>.
    /// </summary>
    /// <param name="request">Code, name, and optionally address, coordinates and exam rooms.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("branches")]
    [ProducesResponseType<BranchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchDto>> CreateBranch(
        [FromBody] CreateBranchRequest request,
        [FromServices] CreateBranchHandler handler,
        CancellationToken cancellationToken)
    {
        var branch = await handler.HandleAsync(
            new CreateBranchCommand(
                request.Code,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.ExamRooms),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(branch.Version);

        return Created("/clinic/branches", branch);
    }

    /// <summary>
    /// <c>PATCH /clinic/branches/{id}</c> — a branch's name, address, pin or exam rooms. The code
    /// stays. A room that a ticket still on the queue is in cannot be taken away
    /// (<c>409 exam_room_in_use</c>). <b>Contract extension</b>, see
    /// <c>docs/contract-extensions.md § 51</c>.
    /// </summary>
    /// <param name="id">The branch — <c>brn_sukhumvit39</c>.</param>
    /// <param name="request">The parts to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("branches/{id}")]
    [ProducesResponseType<BranchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BranchDto>> UpdateBranch(
        string id,
        [FromBody] UpdateBranchRequest request,
        [FromServices] UpdateBranchHandler handler,
        CancellationToken cancellationToken)
    {
        var branch = await handler.HandleAsync(
            new UpdateBranchCommand(
                id,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.ExamRooms,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(branch.Version);

        return Ok(branch);
    }

    /// <summary>
    /// <c>PUT /clinic/hours</c> — "ตั้งทั้งสัปดาห์ในคำขอเดียว". A whole-week replacement: all seven
    /// days, each exactly once.
    /// </summary>
    /// <param name="request">The week.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("hours")]
    [ProducesResponseType<OpeningHoursDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OpeningHoursDto>> ReplaceHours(
        [FromBody] ReplaceOpeningHoursRequest request,
        [FromServices] ReplaceOpeningHoursHandler handler,
        CancellationToken cancellationToken)
    {
        var hours = await handler.HandleAsync(
            new ReplaceOpeningHoursCommand(
                [.. request.Days.Select(day => new DayHoursInput(day.Day, day.Opens, day.Closes, day.Closed))],
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(hours.Version);

        return Ok(hours);
    }

    /// <summary>
    /// <c>POST /clinic/holidays</c> — "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ".
    /// <para>
    /// Declaring the day is what closes booking: the availability feed offers nothing on it and a
    /// booking for it answers <c>422 slot_unavailable</c>. Appointments already made are not
    /// touched.
    /// </para>
    /// </summary>
    /// <param name="request">The day, what it is, and optionally the one branch it shuts.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("holidays")]
    [ProducesResponseType<HolidayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HolidayDto>> AddHoliday(
        [FromBody] AddHolidayRequest request,
        [FromServices] AddHolidayHandler handler,
        CancellationToken cancellationToken)
    {
        var holiday = await handler.HandleAsync(
            new AddHolidayCommand(request.Date, request.Name, request.BranchId), cancellationToken);

        return Created("/clinic/hours", holiday);
    }
}
