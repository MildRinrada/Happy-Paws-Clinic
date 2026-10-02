using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.RecordAllergy;

/// <summary>
/// <c>POST /staff/pets/{id}/allergies</c> —
/// "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง".
/// </summary>
public sealed record RecordAllergyCommand(
    string PetId,
    string Substance,
    string Severity,
    string? Reaction = null);

/// <summary>
/// Records an allergy against a patient.
/// <para>
/// Built alongside the consultation module rather than with the rest of the Tier-2 patient
/// endpoints, because the prescription endpoint is required to check allergies and had nothing to
/// check against. This is the write side of that check.
/// </para>
/// </summary>
public sealed class RecordAllergyHandler(
    IPetRepository pets,
    IPetAllergyRepository allergies,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AllergyDto> HandleAsync(
        RecordAllergyCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var severity = ParseSeverity(command.Severity);

        PetAllergy? recorded = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            var allergy = PetAllergy.Record(
                pet.Id, command.Substance, severity, staffId, clock.UtcNow, command.Reaction);

            allergies.Add(allergy);

            audit.Record(new AuditEvent(
                AuditActions.AllergyRecorded,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["allergy_id"] = allergy.Id,
                    ["substance"] = allergy.Substance,
                    ["severity"] = allergy.Severity.ToString(),
                }));

            recorded = allergy;
        }, cancellationToken);

        return AllergyDto.From(recorded!);
    }

    private static AllergySeverity ParseSeverity(string value) =>
        Enum.TryParse<AllergySeverity>(value, ignoreCase: true, out var severity)
            ? severity
            : throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["severity"] = ["ระดับความรุนแรงต้องเป็น mild, moderate หรือ severe"],
            });
}
