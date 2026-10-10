using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.UpdatePet;

/// <summary>
/// <c>PATCH /staff/pets/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 28</c>. An absent field is left alone; an empty string clears
/// breed or chip; <c>Clear</c> names <c>birth_date</c> or <c>neutered</c> to set them back to unknown.
/// </summary>
/// <param name="PetId">The patient.</param>
/// <param name="Name">The name on every screen and document.</param>
/// <param name="Species">Free text, as at registration.</param>
/// <param name="Breed">Empty clears it.</param>
/// <param name="MicrochipNumber">Empty clears it. Unique across the clinic.</param>
/// <param name="Sex"><c>male</c>, <c>female</c> or <c>unknown</c>.</param>
/// <param name="BirthDate">Not in the future.</param>
/// <param name="BirthDateEstimated">True when the birth date is a guess.</param>
/// <param name="Neutered">Neutered or spayed.</param>
/// <param name="Clear"><c>birth_date</c> and/or <c>neutered</c>, to forget them.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>; required.</param>
public sealed record UpdatePetCommand(
    string PetId,
    string? Name = null,
    string? Species = null,
    string? Breed = null,
    string? MicrochipNumber = null,
    string? Sex = null,
    DateOnly? BirthDate = null,
    bool? BirthDateEstimated = null,
    bool? Neutered = null,
    IReadOnlyList<string>? Clear = null,
    uint? IfMatchVersion = null);

public sealed class UpdatePetValidator : AbstractValidator<UpdatePetCommand>
{
    /// <summary>What <c>clear</c> may name. Text fields are cleared with an empty string instead.</summary>
    public static readonly IReadOnlySet<string> Clearable = new HashSet<string>(StringComparer.Ordinal) { "birth_date", "neutered" };

    public UpdatePetValidator(IClock clock)
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("ต้องระบุชื่อสัตว์เลี้ยง")
            .MaximumLength(120).WithMessage("ชื่อสัตว์เลี้ยงยาวได้ไม่เกิน 120 ตัวอักษร")
            .When(command => command.Name is not null);

        RuleFor(command => command.Species)
            .NotEmpty().WithMessage("ต้องระบุชนิดสัตว์")
            .MaximumLength(60).WithMessage("ชนิดสัตว์ยาวได้ไม่เกิน 60 ตัวอักษร")
            .When(command => command.Species is not null);

        RuleFor(command => command.Breed).MaximumLength(120).WithMessage("สายพันธุ์ยาวได้ไม่เกิน 120 ตัวอักษร");
        RuleFor(command => command.MicrochipNumber).MaximumLength(40).WithMessage("เลขไมโครชิปยาวได้ไม่เกิน 40 ตัวอักษร");
        RuleFor(command => command.Sex).MustBeASex();
        RuleFor(command => command.BirthDate).MustBeABirthDate(clock);

        RuleFor(command => command.Clear)
            .Must(fields => fields is null || fields.All(Clearable.Contains))
            .WithMessage($"clear รับได้เฉพาะ: {string.Join(", ", Clearable)}");

        RuleFor(command => command.BirthDate)
            .Null().WithMessage("ส่งวันเกิดพร้อมกับลบวันเกิดในคำขอเดียวกันไม่ได้")
            .When(command => command.Clear?.Contains("birth_date") is true);

        RuleFor(command => command.Neutered)
            .Null().WithMessage("ส่งสถานะทำหมันพร้อมกับลบสถานะทำหมันในคำขอเดียวกันไม่ได้")
            .When(command => command.Clear?.Contains("neutered") is true);
    }
}

/// <summary>
/// Staff's correction of a patient's identity card: name, species, breed, chip, sex, birth date and
/// neuter status.
/// <para>
/// <b>The owner's four fields are not here.</b> Nickname, photo, diet and behaviour are the owner's
/// words about their own animal and stay on <c>PATCH /portal/pets/{id}</c>; the command has nowhere
/// to put them. What the clinic wants to say about behaviour goes into the medical record.
/// </para>
/// <para>
/// <b>A closed record can still be corrected</b>, because its history is read and printed under
/// this name, breed and chip. Its neuter status cannot change (the domain refuses with
/// <c>409 state_conflict</c>), and the owner-side edit stays refused as before.
/// </para>
/// <para>
/// One audit row per request that changes something, with each changed field before and after:
/// none of these is sensitive the way a diagnosis is, and "who changed the chip number" is
/// exactly the question the row is there to answer. A request that changes nothing writes nothing.
/// </para>
/// </summary>
public sealed class UpdatePetHandler(
    IPetRepository pets,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<PetSummaryDto> HandleAsync(UpdatePetCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Pet? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindTrackedByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            ConcurrencyGuard.EnsureRequired(pet, command.IfMatchVersion);

            var was = Snapshot(pet);

            await EnsureMicrochipIsFreeAsync(pet, command.MicrochipNumber, token);

            var clearsBirthDate = command.Clear?.Contains("birth_date") is true;

            if (command.BirthDateEstimated is true && (clearsBirthDate || (command.BirthDate ?? pet.BirthDate) is null))
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["birth_date_estimated"] = ["ระบุว่าเป็นวันเกิดโดยประมาณได้เมื่อมีวันเกิดเท่านั้น"],
                });
            }

            pet.Revise(
                new PetRevision(
                    command.Name,
                    command.Species,
                    command.Breed,
                    command.MicrochipNumber,
                    PetProfileRules.SexOf(command.Sex),
                    command.BirthDate,
                    command.BirthDateEstimated,
                    ClearBirthDate: clearsBirthDate,
                    command.Neutered,
                    ClearNeutered: command.Clear?.Contains("neutered") is true),
                PetProfileRules.Today(clock));

            var now = Snapshot(pet);
            var changes = new Dictionary<string, object?>();

            foreach (var (field, before) in was)
            {
                var after = now[field];

                if (!Equals(before, after))
                {
                    changes[field] = new Dictionary<string, object?> { ["before"] = before, ["after"] = after };
                }
            }

            if (changes.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.PetUpdated,
                    AuditResources.Pet,
                    pet.Id,
                    pet.Id,
                    Details: new Dictionary<string, object?>
                    {
                        ["hospital_number"] = pet.HospitalNumber,
                        ["deceased"] = pet.IsDeceased,
                        ["changes"] = changes,
                    }));
            }

            updated = pet;
        }, cancellationToken);

        return PetSummaryDto.From(updated!);
    }

    /// <summary>The audited fields, under their wire names, in values that compare with <c>Equals</c>.</summary>
    private static Dictionary<string, object?> Snapshot(Pet pet) => new(StringComparer.Ordinal)
    {
        ["name"] = pet.Name,
        ["species"] = pet.Species,
        ["breed"] = pet.Breed,
        ["microchip_number"] = pet.MicrochipNumber,
        ["sex"] = WireEnum.Of(pet.Sex),
        ["birth_date"] = pet.BirthDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        ["birth_date_estimated"] = pet.BirthDateEstimated,
        ["neutered"] = pet.Neutered,
    };

    /// <summary>
    /// The same refusal, on the same field, as registering a patient: a chip number identifies
    /// exactly one animal. The patient's own number is not a clash with itself.
    /// </summary>
    private async Task EnsureMicrochipIsFreeAsync(Pet pet, string? microchip, CancellationToken cancellationToken)
    {
        var typed = microchip?.Trim();

        if (typed is not { Length: > 0 } || typed == pet.MicrochipNumber)
        {
            return;
        }

        if (await pets.MicrochipExistsAsync(typed, cancellationToken))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["microchip_number"] = ["ไมโครชิปนี้ถูกใช้กับคนไข้รายอื่นแล้ว"],
            });
        }
    }
}
