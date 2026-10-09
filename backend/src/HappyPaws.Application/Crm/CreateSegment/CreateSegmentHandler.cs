using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.CreateSegment;

/// <summary><c>POST /staff/segments</c> — "กลุ่มลูกค้าจากเงื่อนไข เช่น ไม่มา 6 เดือน".</summary>
public sealed record CreateSegmentCommand(string Name, SegmentCriteriaInput? Criteria);

/// <summary>The conditions as they arrive. See <see cref="SegmentCriteria"/> for what each means.</summary>
public sealed record SegmentCriteriaInput(
    IReadOnlyList<string>? Species = null,
    int? InactiveDays = null,
    int? VisitedWithinDays = null,
    long? MinSpendSatang = null,
    IReadOnlyList<string>? Tiers = null,
    bool? HasActivePackage = null,
    string? BranchId = null);

public sealed class CreateSegmentValidator : AbstractValidator<CreateSegmentCommand>
{
    public const int MaxSpecies = 10;
    public const int MaxDays = 3_650;

    public CreateSegmentValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("ต้องระบุชื่อกลุ่ม").MaximumLength(120);
        RuleFor(command => command.Criteria).NotNull().WithMessage("ต้องมีเงื่อนไขอย่างน้อยหนึ่งข้อ");

        When(command => command.Criteria is not null, () =>
        {
            RuleFor(command => command.Criteria!.Species)
                .Must(species => species is null || species.Count <= MaxSpecies)
                .OverridePropertyName("criteria.species")
                .WithMessage($"ระบุชนิดสัตว์ได้ไม่เกิน {MaxSpecies} ชนิด");
            RuleForEach(command => command.Criteria!.Species).NotEmpty().MaximumLength(60).OverridePropertyName("criteria.species");

            RuleFor(command => command.Criteria!.InactiveDays)
                .InclusiveBetween(1, MaxDays).When(command => command.Criteria!.InactiveDays.HasValue)
                .OverridePropertyName("criteria.inactive_days");
            RuleFor(command => command.Criteria!.VisitedWithinDays)
                .InclusiveBetween(1, MaxDays).When(command => command.Criteria!.VisitedWithinDays.HasValue)
                .OverridePropertyName("criteria.visited_within_days");
            RuleFor(command => command.Criteria!.MinSpendSatang)
                .GreaterThan(0).When(command => command.Criteria!.MinSpendSatang.HasValue)
                .OverridePropertyName("criteria.min_spend_satang");

            RuleForEach(command => command.Criteria!.Tiers)
                .Must(tier => CrmWire.ParseTier(tier) is not null)
                .WithMessage("ระดับสมาชิกต้องเป็น member, silver หรือ gold")
                .OverridePropertyName("criteria.tiers");

            RuleFor(command => command.Criteria!)
                .Must(criteria => criteria.InactiveDays is null || criteria.VisitedWithinDays is null
                    || criteria.VisitedWithinDays > criteria.InactiveDays)
                .OverridePropertyName("criteria.visited_within_days")
                .WithMessage("จำนวนวันที่มาล่าสุดต้องมากกว่าจำนวนวันที่ไม่มา");
        });
    }
}

/// <summary>
/// Saves a customer group and counts it.
/// <para>
/// <b>Branch.</b> A group limited to one branch needs access to that branch. A group over every
/// branch can only be made by someone not bound to branches, because its count describes customers
/// of branches a branch-bound account cannot see.
/// </para>
/// </summary>
public sealed class CreateSegmentHandler(
    ISegmentRepository segments,
    ISegmentQuery query,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<SegmentDto> HandleAsync(
        CreateSegmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var input = command.Criteria!;

        var branchId = await CrmBranches.ResolveOptionalAsync(catalog, input.BranchId, cancellationToken);

        if (branchId is null)
        {
            if (branchGuard.VisibleBranchIds is not null)
            {
                throw new ForbiddenException("a branch-bound staff member cannot make a segment over every branch");
            }
        }
        else
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var criteria = new SegmentCriteria(
            input.Species ?? [],
            input.InactiveDays,
            input.VisitedWithinDays,
            input.MinSpendSatang,
            [.. (input.Tiers ?? []).Select(tier => CrmWire.ParseTier(tier)!.Value)],
            input.HasActivePackage,
            branchId);

        if (criteria.IsEmpty)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["criteria"] = ["ต้องมีเงื่อนไขอย่างน้อยหนึ่งข้อ"],
            });
        }

        var segment = Segment.Create(command.Name, criteria, staffId);
        var counts = await query.CountAsync(segment.Criteria, clinicDays.Today, cancellationToken);
        segment.RecordCounts(counts.Customers, counts.Reachable);

        await unitOfWork.ExecuteInTransactionAsync(token =>
        {
            segments.Add(segment);

            audit.Record(new AuditEvent(
                AuditActions.SegmentCreated,
                AuditResources.Segment,
                segment.Id,
                BranchId: branchId,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = segment.Name,
                    ["customer_count"] = segment.CustomerCount,
                    ["reachable_count"] = segment.ReachableCount,
                }));

            return Task.CompletedTask;
        }, cancellationToken);

        return SegmentDto.From(segment);
    }
}
