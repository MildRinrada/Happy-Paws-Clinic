using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.ManageCarers;

/// <summary>
/// <c>POST /staff/pets/{id}/owners</c> — "เพิ่มผู้ดูแลร่วม · กำหนดสิทธิ์ดูประวัติ".
/// </summary>
/// <param name="PetId">The patient.</param>
/// <param name="CustomerId">The person being given access.</param>
/// <param name="MayViewHistory">
/// "กำหนดสิทธิ์ดูประวัติ" — set separately from the link itself, so a relative who brings the
/// animal in can book and collect it without reading its medical record.
/// </param>
public sealed record AddCarerCommand(string PetId, string CustomerId, bool MayViewHistory = true);

/// <summary><c>DELETE /staff/pets/{id}/owners/{cid}</c> — "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ".</summary>
public sealed record RemoveCarerCommand(string PetId, string CustomerId);

/// <summary>
/// Grants a person access to a patient.
/// <para>
/// Re-adding an existing carer updates their history permission rather than failing. The counter's
/// intent in both cases is "this person should have this access", and a 409 would make the caller
/// branch on a distinction nobody outside the database cares about.
/// </para>
/// <para>
/// Audited as <c>pet.access_granted</c>. This is the clearest case of the registry's second
/// admission rule — it changes who can do what — and the thing it changes is who can read an
/// animal's medical history from the portal.
/// </para>
/// </summary>
public sealed class AddCarerHandler(
    IPetRepository pets,
    IPetOwnershipRepository ownerships,
    ICustomerRepository customers,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<CarerDto> HandleAsync(
        AddCarerCommand command,
        CancellationToken cancellationToken = default)
    {
        PetOwnership? link = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            var customer = await customers.FindByIdAsync(command.CustomerId, token)
                ?? throw new NotFoundException("customer", command.CustomerId);

            var existing = await ownerships.FindActiveAsync(pet.Id, customer.Id, token);

            if (existing is null)
            {
                link = PetOwnership.Create(pet.Id, customer.Id, isPrimary: false, command.MayViewHistory);
                ownerships.Add(link);
            }
            else
            {
                existing.SetHistoryAccess(command.MayViewHistory);
                link = existing;
            }

            audit.Record(new AuditEvent(
                AuditActions.PetAccessGranted,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["customer_id"] = customer.Id,
                    ["may_view_history"] = command.MayViewHistory,
                }));
        }, cancellationToken);

        return CarerDto.From(link!);
    }
}

/// <summary>
/// Withdraws a person's access to a patient.
/// <para>
/// "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ" — the link is <b>closed</b>, never deleted. Who could see what,
/// and until when, stays answerable, which matters because withdrawal usually follows a dispute and
/// a dispute is exactly when someone asks.
/// </para>
/// <para>
/// The primary carer cannot be removed. Doing so would orphan the record and, with it, every
/// notification and invoice that hangs off the owner; the aggregate refuses with
/// <c>primary_owner_required</c>.
/// </para>
/// <para>
/// Access is re-checked against the database on every portal request rather than carried in the
/// token, so a withdrawal takes effect on the next call rather than when a fifteen-minute token
/// expires. See <c>PetAccessPolicy</c>.
/// </para>
/// </summary>
public sealed class RemoveCarerHandler(
    IPetOwnershipRepository ownerships,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(RemoveCarerCommand command, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var link = await ownerships.FindActiveAsync(command.PetId, command.CustomerId, token)
                ?? throw new NotFoundException("pet_ownership", $"{command.PetId}/{command.CustomerId}");

            link.Revoke(clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.PetAccessRevoked,
                AuditResources.Pet,
                command.PetId,
                command.PetId,
                Details: new Dictionary<string, object?> { ["customer_id"] = command.CustomerId }));
        }, cancellationToken);
    }
}
