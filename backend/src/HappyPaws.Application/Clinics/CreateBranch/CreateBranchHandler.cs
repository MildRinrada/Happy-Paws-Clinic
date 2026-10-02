using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Clinics.CreateBranch;

/// <summary><c>POST /clinic/branches</c> — "เปิดสาขาใหม่ในคลินิกเดียวกัน".</summary>
/// <param name="Code">Short lowercase code — <c>sukhumvit39</c>. Becomes the id, <c>brn_sukhumvit39</c>.</param>
/// <param name="Name">The branch as customers know it.</param>
/// <param name="Address">Street address.</param>
/// <param name="Latitude">Map position; sent with <paramref name="Longitude"/> or not at all.</param>
/// <param name="Longitude">Map position.</param>
/// <param name="ExamRooms">Consulting rooms — <c>room_1</c>, <c>room_2</c>.</param>
public sealed record CreateBranchCommand(
    string Code,
    string Name,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    IReadOnlyList<string>? ExamRooms = null);

public sealed class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        // The code is the readable half of the id the contract shows (brn_sukhumvit39), so it is
        // held to what is safe in a URL and what fits the id column.
        RuleFor(command => command.Code)
            .NotEmpty()
            .Matches("^[a-z0-9][a-z0-9_-]{1,31}$")
            .WithMessage("รหัสสาขาต้องเป็น a–z, 0–9, _ หรือ - ยาว 2–32 ตัวอักษร");

        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Address).MaximumLength(500);

        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180);

        RuleFor(command => command)
            .Must(command => (command.Latitude is null) == (command.Longitude is null))
            .OverridePropertyName(nameof(CreateBranchCommand.Latitude))
            .WithMessage("พิกัดต้องระบุทั้งละติจูดและลองจิจูด");

        RuleForEach(command => command.ExamRooms).NotEmpty().MaximumLength(60);
    }
}

/// <summary>
/// Opens a new site.
/// <para>
/// The id is derived from the code rather than generated, which is what the contract's own
/// examples do — <c>brn_sukhumvit39</c> appears in a token, in a URL and in a support ticket, and
/// all three are easier to read than a random suffix. Branches are created a handful of times in a
/// clinic's life, so there is no collision to worry about beyond the one the unique code already
/// refuses.
/// </para>
/// </summary>
public sealed class CreateBranchHandler(
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<BranchDto> HandleAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Branch? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await branches.CodeExistsAsync(command.Code, token))
            {
                throw new DuplicateBranchException(command.Code);
            }

            created = Branch.Create(
                command.Code,
                command.Name,
                id: $"{EntityId.Branch}_{command.Code}",
                examRooms: command.ExamRooms,
                address: command.Address,
                latitude: command.Latitude,
                longitude: command.Longitude);

            branches.Add(created);
        }, cancellationToken);

        return BranchDto.From(created!);
    }
}

/// <summary><c>409 duplicate_branch</c> — the code is taken. Not in the API reference's error list.</summary>
public sealed class DuplicateBranchException : AppException
{
    public DuplicateBranchException(string code)
        : base("duplicate_branch", "มีสาขาที่ใช้รหัสนี้อยู่แล้ว", HttpStatus.Conflict)
    {
        MessageEn = $"A branch with code '{code}' already exists.";
        Field = "code";
    }
}
