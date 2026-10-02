using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Clinics;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.AddHoliday;
using HappyPaws.Application.Clinics.CreateBranch;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.ReplaceOpeningHours;
using HappyPaws.Application.Clinics.UpdateClinicProfile;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The writes behind the public clinic pages — profile, branches, hours and holidays.
/// <para>
/// Four routes, one scope. The contract puts every one of them on <c>settings:write</c> ("แก้ได้
/// เฉพาะเจ้าของและแอดมิน"), and the reads they pair with are public and live in
/// <c>Public/ClinicController</c>.
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
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(profile.Version);

        return Ok(profile);
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
