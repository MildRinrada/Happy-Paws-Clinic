using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.AddHoliday;

/// <summary><c>POST /clinic/holidays</c> — "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ".</summary>
/// <param name="Date">The clinic-local day that is shut.</param>
/// <param name="Name">What the day is, as customers will read it.</param>
/// <param name="BranchId">One branch, or omitted to shut every branch.</param>
public sealed record AddHolidayCommand(DateOnly Date, string Name, string? BranchId = null);

public sealed class AddHolidayValidator : AbstractValidator<AddHolidayCommand>
{
    public AddHolidayValidator()
    {
        RuleFor(command => command.Date).NotEqual(default(DateOnly)).WithMessage("ต้องระบุวันที่");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}

/// <summary>
/// Declares a day the clinic is shut.
/// <para>
/// "ปิดรับนัดอัตโนมัติ" needs nothing further from this handler. The availability feed and the slot
/// resolver both ask the calendar whether the day is a holiday, so declaring one is what closes
/// booking — there is no second switch to remember and no list of booking paths to keep in step.
/// </para>
/// <para>
/// Appointments already on that day are left alone. Whether to move them is a conversation with
/// each owner, and cancelling a clinic day's bookings as a side effect of a settings form is not a
/// decision a <c>POST</c> should make on anyone's behalf.
/// </para>
/// </summary>
public sealed class AddHolidayHandler(
    IHolidayRepository holidays,
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<HolidayDto> HandleAsync(
        AddHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command.Date < DateOnly.FromDateTime(clock.NowInClinicTime().DateTime))
        {
            // A closure in the past closes nothing. It is almost always a mistyped year.
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["date"] = ["วันหยุดต้องไม่เป็นวันที่ผ่านมาแล้ว"] });
        }

        ClinicHoliday? declared = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (command.BranchId is { Length: > 0 } branchId
                && (await branches.MissingAsync([branchId], token)).Count > 0)
            {
                throw new ValidationFailedException(
                    new Dictionary<string, string[]> { ["branch_id"] = ["ไม่พบสาขาที่ระบุ"] });
            }

            if (await holidays.ExistsAsync(command.Date, command.BranchId, token))
            {
                throw new DuplicateHolidayException();
            }

            declared = ClinicHoliday.Declare(command.Date, command.Name, command.BranchId);
            holidays.Add(declared);
        }, cancellationToken);

        return HolidayDto.From(declared!);
    }
}

/// <summary><c>409 duplicate_holiday</c> — that day is already declared for that branch.</summary>
public sealed class DuplicateHolidayException : AppException
{
    public DuplicateHolidayException()
        : base("duplicate_holiday", "วันหยุดนี้ถูกประกาศไว้แล้ว", HttpStatus.Conflict)
    {
        MessageEn = "This holiday has already been declared.";
        Field = "date";
    }
}
