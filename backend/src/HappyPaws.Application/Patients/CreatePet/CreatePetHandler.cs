using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.CreatePet;

/// <summary>
/// <c>POST /staff/pets</c> — "ออก HN อัตโนมัติ · ผูกเจ้าของหลัก".
/// </summary>
public sealed record CreatePetCommand(
    string Name,
    string Species,
    string PrimaryCustomerId,
    string? Breed = null,
    string? MicrochipNumber = null);

public sealed class CreatePetValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Species).NotEmpty().MaximumLength(60);
        RuleFor(command => command.PrimaryCustomerId).NotEmpty();
        RuleFor(command => command.MicrochipNumber).MaximumLength(40);
    }
}

/// <summary>
/// Registers a patient.
/// <para>
/// Both halves of "ออก HN อัตโนมัติ · ผูกเจ้าของหลัก" happen in one transaction, and the second is
/// the one worth explaining. The primary owner is written <b>twice</b> — once as
/// <c>Pet.PrimaryCustomerId</c>, which records who the record was opened under and who a visit is
/// billed to, and once as a <see cref="PetOwnership"/> row, which is what decides who may see the
/// animal's history. They are different questions and they diverge in practice: a pet can be
/// rehomed, and a co-owner can be given access without ever becoming the billing contact.
/// </para>
/// <para>
/// <b>The record carries only what the contract names.</b> <c>GET /staff/pets/{id}</c> asks for
/// "สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป", so that is the shape of this. Date of birth, sex and
/// neuter status are all things a real veterinary record wants and none of them is in the reference
/// — adding them would be inventing schema. Flagged in <c>docs/assumptions.md § 25</c> rather than
/// guessed at.
/// </para>
/// </summary>
public sealed class CreatePetHandler(
    IPetRepository pets,
    IPetOwnershipRepository ownerships,
    ICustomerRepository customers,
    IHospitalNumberGenerator hospitalNumbers,
    IAuditTrail audit,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<PetSummaryDto> HandleAsync(
        CreatePetCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Pet? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var owner = await customers.FindByIdAsync(command.PrimaryCustomerId, token)
                ?? throw new NotFoundException("customer", command.PrimaryCustomerId);

            await EnsureMicrochipIsFreeAsync(command.MicrochipNumber, token);

            created = Pet.Create(
                await hospitalNumbers.NextAsync(clock.UtcNow, token),
                command.Name,
                command.Species,
                owner.Id,
                command.Breed,
                command.MicrochipNumber);

            pets.Add(created);
            webhooks.Enqueue(WebhookEventTypes.PetCreated, WebhookPayloads.Pet(created, clock.UtcNow));

            // The access-control half. Primary, and able to read the history — this is the person
            // who brought the animal in.
            ownerships.Add(PetOwnership.Create(created.Id, owner.Id, isPrimary: true, mayViewHistory: true));

            audit.Record(new AuditEvent(
                AuditActions.PetCreated,
                AuditResources.Pet,
                created.Id,
                created.Id,
                Details: new Dictionary<string, object?>
                {
                    ["hospital_number"] = created.HospitalNumber,
                    ["name"] = created.Name,
                    ["primary_customer_id"] = owner.Id,
                }));
        }, cancellationToken);

        return PetSummaryDto.From(created!);
    }

    /// <summary>
    /// A chip number identifies exactly one animal in the world, so a second patient carrying it is
    /// a typo or a patient already registered — and both are worth stopping before a duplicate
    /// clinical record exists.
    /// </summary>
    private async Task EnsureMicrochipIsFreeAsync(string? microchip, CancellationToken cancellationToken)
    {
        if (microchip is not { Length: > 0 })
        {
            return;
        }

        if (await pets.MicrochipExistsAsync(microchip, cancellationToken))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["microchip_number"] = ["ไมโครชิปนี้ถูกใช้กับคนไข้รายอื่นแล้ว"],
            });
        }
    }
}
