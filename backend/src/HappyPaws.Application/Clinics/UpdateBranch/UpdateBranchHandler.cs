using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.UpdateBranch;

/// <summary>
/// <c>PATCH /clinic/branches/{id}</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 51</c>. A field that is absent is left alone; an empty
/// <c>address</c> clears it. <c>ExamRooms</c>, when sent, is the whole list.
/// </summary>
/// <param name="BranchId">From the route — <c>brn_sukhumvit39</c>.</param>
/// <param name="Name">The branch as customers know it.</param>
/// <param name="Address">Street address; empty clears it.</param>
/// <param name="Latitude">Sent with <paramref name="Longitude"/> or not at all.</param>
/// <param name="Longitude">Map position.</param>
/// <param name="ExamRooms">Every room the branch has after the edit.</param>
/// <param name="IfMatchVersion">The version the client read; honoured when sent.</param>
public sealed record UpdateBranchCommand(
    string BranchId,
    string? Name = null,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    IReadOnlyList<string>? ExamRooms = null,
    uint? IfMatchVersion = null);

/// <summary>The same limits as <c>POST /clinic/branches</c>, applied only to what was sent.</summary>
public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().MaximumLength(200)
            .When(command => command.Name is not null);

        RuleFor(command => command.Address).MaximumLength(500);

        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180);

        RuleFor(command => command)
            .Must(command => (command.Latitude is null) == (command.Longitude is null))
            .OverridePropertyName(nameof(UpdateBranchCommand.Latitude))
            .WithMessage("พิกัดต้องระบุทั้งละติจูดและลองจิจูด");

        RuleForEach(command => command.ExamRooms).NotEmpty().MaximumLength(60);
    }
}

/// <summary>
/// Changes a branch's name, address, pin or exam rooms. The code never changes: it is the readable
/// half of the id, already printed on tickets, in tokens and in links.
/// <para>
/// <b>A room cannot be taken away while the queue still uses it.</b> Exam rooms are referenced by
/// queue tickets only (<c>QueueTicket.RoomId</c>; appointments carry no room), so a room is in use
/// when a ticket still on the board, today or later, is assigned to it. Removing it then would
/// leave a waiting owner told to go to a door the registry no longer has, and
/// <c>POST /staff/queue/{id}/assign</c> could not move them back. The edit is refused whole with
/// <c>409 exam_room_in_use</c>, naming the rooms and the tickets; adding rooms is never blocked.
/// </para>
/// <para>
/// <c>settings:write</c>, like opening a branch. A caller assigned to particular branches may
/// change only those (<see cref="BranchGuard"/>); an owner or a token with no branch list may
/// change any. <c>If-Match</c> is honoured but not demanded, the same as the rest of clinic setup.
/// </para>
/// </summary>
public sealed class UpdateBranchHandler(
    IBranchRepository branches,
    BranchGuard branchGuard,
    IClock clock,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<BranchDto> HandleAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Branch? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branch = await branches.FindOpenTrackedAsync(command.BranchId, token)
                ?? throw new NotFoundException("branch", command.BranchId);

            branchGuard.EnsureCanAccess(branch.Id);
            ConcurrencyGuard.Ensure(branch, command.IfMatchVersion);

            if (command.ExamRooms is { } rooms && branch.RoomsRemovedBy(rooms) is { Count: > 0 } removed)
            {
                var uses = await branches.ExamRoomUsesAsync(branch.Id, removed, ClinicToday(), token);

                if (uses.Count > 0)
                {
                    throw InUse(uses);
                }
            }

            // A copy of the rooms: the DTO holds the entity's own list, which the edit refills.
            var before = BranchDto.From(branch) with { ExamRooms = [.. branch.ExamRooms] };

            branch.Revise(command.Name, command.Address, command.Latitude, command.Longitude, command.ExamRooms);

            var changes = Changes(before, BranchDto.From(branch));

            if (changes.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.BranchUpdated,
                    AuditResources.Branch,
                    branch.Id,
                    BranchId: branch.Id,
                    Details: changes));
            }

            updated = branch;
        }, cancellationToken);

        return BranchDto.From(updated!);
    }

    private DateOnly ClinicToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, clock.ClinicTimeZone).DateTime);

    private static ConflictException InUse(IReadOnlyList<ExamRoomUse> uses)
    {
        var rooms = uses.Select(use => use.RoomId).Distinct(StringComparer.Ordinal).ToList();

        // The first few numbers are enough for the counter to find them on the board.
        var numbers = string.Join(", ", uses.Select(use => use.Number).Take(5));
        var more = uses.Count > 5 ? " และคิวอื่น ๆ" : string.Empty;

        return new ConflictException(
            ErrorCodes.ExamRoomInUse,
            $"ยังเอาห้อง {string.Join(", ", rooms)} ออกไม่ได้ เพราะคิว {numbers}{more} ยังอยู่ในห้องนั้น ย้ายคิวไปห้องอื่นหรือรอให้เสร็จก่อน")
        {
            MessageEn = "An exam room being removed is assigned to a ticket still on the queue.",
            Field = "exam_rooms",
            Details = new Dictionary<string, object>
            {
                ["rooms"] = rooms,
                ["tickets"] = uses
                    .Select(use => new Dictionary<string, object>
                    {
                        ["id"] = use.TicketId,
                        ["number"] = use.Number,
                        ["room_id"] = use.RoomId,
                        ["service_date"] = use.ServiceDate.ToString("yyyy-MM-dd"),
                    })
                    .ToList(),
            },
        };
    }

    /// <summary>Each part that changed, before and after. Nothing changed writes no row.</summary>
    private static Dictionary<string, object?> Changes(BranchDto before, BranchDto after)
    {
        var changes = new Dictionary<string, object?>();

        void Compare(string field, object? was, object? now, bool same)
        {
            if (!same)
            {
                changes[field] = new Dictionary<string, object?> { ["before"] = was, ["after"] = now };
            }
        }

        Compare("name", before.Name, after.Name, before.Name == after.Name);
        Compare("address", before.Address, after.Address, before.Address == after.Address);
        Compare(
            "coordinates",
            Pin(before),
            Pin(after),
            before.Latitude == after.Latitude && before.Longitude == after.Longitude);
        Compare("exam_rooms", before.ExamRooms, after.ExamRooms, before.ExamRooms.SequenceEqual(after.ExamRooms));

        return changes;
    }

    private static double[]? Pin(BranchDto branch) =>
        branch is { Latitude: { } latitude, Longitude: { } longitude } ? [latitude, longitude] : null;
}
