using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Laboratory.CreateLabRequest;

/// <summary>
/// <c>POST /staff/lab-requests</c> — "ส่งแล็บในคลินิกหรือแล็บภายนอก". <c>Destination</c> is
/// <c>in_house</c> or <c>external</c>; <c>Priority</c> is <c>routine</c> (default) or
/// <c>urgent</c>; <c>BranchId</c> is where the sample was taken, ignored when a visit says so.
/// </summary>
public sealed record CreateLabRequestCommand(
    string PetId,
    string Destination,
    IReadOnlyList<LabTest> Tests,
    string? ConsultationId = null,
    string? BranchId = null,
    string? ExternalLabName = null,
    string? Priority = null,
    string? ClinicalNotes = null);

public sealed class CreateLabRequestValidator : AbstractValidator<CreateLabRequestCommand>
{
    public CreateLabRequestValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();

        RuleFor(command => command.Destination)
            .Must(value => WireEnum.TryParse<LabDestination>(value, out _))
            .WithMessage($"destination ต้องเป็น {WireEnum.Names<LabDestination>()}");

        RuleFor(command => command.Priority)
            .Must(value => WireEnum.TryParse<LabPriority>(value, out _))
            .WithMessage($"priority ต้องเป็น {WireEnum.Names<LabPriority>()}")
            .When(command => command.Priority is not null);

        RuleFor(command => command.Tests).NotEmpty();
        RuleForEach(command => command.Tests).ChildRules(test =>
        {
            test.RuleFor(item => item.Name).NotEmpty().MaximumLength(200);
            test.RuleFor(item => item.Code).MaximumLength(50);
        });

        RuleFor(command => command.ExternalLabName).MaximumLength(200);
        RuleFor(command => command.ClinicalNotes).MaximumLength(2_000);
    }
}

/// <summary>
/// Sends a sample for testing.
/// <para>
/// <b>Linked to a visit, not part of it.</b> The sample is usually taken during a consultation, and
/// when it is the request carries that case's id and branch. But results come back after the case
/// is closed and signed, so the request is its own record that the case points to, not a section
/// of the case that would lock with it.
/// </para>
/// <para>
/// The branch matters because the worklist is per branch: a vet at one site does not see another
/// site's pending samples. Without a visit, the branch is the one given, or the caller's own when
/// they work at exactly one.
/// </para>
/// </summary>
public sealed class CreateLabRequestHandler(
    IPetRepository pets,
    IConsultationRepository consultations,
    IBranchRepository branches,
    ILabRequestRepository requests,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(
        CreateLabRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        WireEnum.TryParse<LabDestination>(command.Destination, out var destination);
        var priority = command.Priority is null
            ? LabPriority.Routine
            : WireEnum.TryParse<LabPriority>(command.Priority, out var parsed) ? parsed : LabPriority.Routine;

        LabRequest? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            if (pet.IsDeceased)
            {
                throw new StateConflictException(nameof(Pet), "deceased", "request_lab");
            }

            var branchId = await BranchForAsync(command, pet.Id, token);
            branchGuard.EnsureCanAccess(branchId);

            var request = LabRequest.Create(
                pet.Id,
                branchId,
                staffId,
                clock.UtcNow,
                destination,
                command.Tests,
                priority,
                command.ExternalLabName,
                command.ConsultationId,
                command.ClinicalNotes);

            requests.Add(request);

            audit.Record(new AuditEvent(
                AuditActions.LabRequested,
                AuditResources.LabRequest,
                request.Id,
                pet.Id,
                branchId,
                new Dictionary<string, object?>
                {
                    ["tests"] = request.Tests.Select(test => test.Name).ToArray(),
                    ["destination"] = WireEnum.Of(request.Destination),
                    ["consultation_id"] = request.ConsultationId,
                }));

            created = request;
        }, cancellationToken);

        return LabRequestDto.From(created!);
    }

    private async Task<string> BranchForAsync(CreateLabRequestCommand command, string petId, CancellationToken token)
    {
        if (command.ConsultationId is { Length: > 0 } consultationId)
        {
            var consultation = await consultations.FindByIdAsync(consultationId, token);

            if (consultation is null || consultation.PetId != petId)
            {
                throw new NotFoundException("consultation", consultationId);
            }

            return consultation.BranchId;
        }

        if (command.BranchId is { Length: > 0 } branchId)
        {
            if ((await branches.MissingAsync([branchId], token)).Count > 0)
            {
                throw new NotFoundException("branch", branchId);
            }

            return branchId;
        }

        if (currentUser.BranchIds.Count == 1)
        {
            return currentUser.BranchIds.First();
        }

        var open = await branches.ListActiveAsync(token);

        return open.Count == 1
            ? open[0].Id
            : throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["branch_id"] = ["คลินิกมีหลายสาขา ต้องระบุสาขาที่เก็บตัวอย่าง"],
            });
    }
}
