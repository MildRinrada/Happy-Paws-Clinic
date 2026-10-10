using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>
/// <c>POST /staff/queue/displays</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 30</c>.
/// <code>{ "name": "ทีวีห้องรอชั้น 1", "branch_id": "brn_…", "expires_at": null }</code>
/// </summary>
public sealed record CreateQueueDisplayCommand(string Name, string BranchId, DateTimeOffset? ExpiresAt = null);

public sealed class CreateQueueDisplayValidator : AbstractValidator<CreateQueueDisplayCommand>
{
    public CreateQueueDisplayValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("ต้องตั้งชื่อจอ")
            .MaximumLength(QueueDisplay.NameMaxLength).WithMessage("ชื่อจอยาวได้ไม่เกิน 80 ตัวอักษร");

        RuleFor(command => command.BranchId)
            .NotEmpty().WithMessage("ต้องเลือกสาขา")
            .MaximumLength(40);
    }
}

/// <summary>
/// Makes a waiting-room screen for one branch and shows its setup code once.
/// <para>
/// <b>Who.</b> <c>queue:write</c>, the scope that already organises the board, within the caller's
/// branches (<c>403</c> for another). The branch must exist and be open (<c>422</c> at
/// <c>branch_id</c>).
/// </para>
/// <para>
/// <b>What comes back.</b> Not the token: a short code that the TV trades for it at
/// <c>POST /queue/displays/claim</c> within 30 minutes, once. The token then lives only on the TV's
/// server, so it is never on a screen, in a link or in anyone's clipboard. Only hashes are stored.
/// </para>
/// <para>
/// <b>End.</b> <c>expires_at</c> is optional, up to two years; without it the display works until
/// it is revoked.
/// </para>
/// </summary>
public sealed class CreateQueueDisplayHandler(
    IQueueDisplayRepository displays,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<CreatedQueueDisplayDto> HandleAsync(
        CreateQueueDisplayCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var branchId = command.BranchId.Trim();

        branchGuard.EnsureCanAccess(branchId);

        var branch = await catalog.FindBranchAsync(branchId, cancellationToken);
        if (branch is not { IsActive: true })
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["branch_id"] = ["ไม่พบสาขานี้ หรือสาขาปิดอยู่"],
            });
        }

        var now = clock.UtcNow;

        if (command.ExpiresAt is { } end && (end <= now || end > now + QueueDisplay.MaxLifetime))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["expires_at"] = ["วันหมดอายุต้องอยู่ในอนาคตและไม่เกิน 2 ปีนับจากวันนี้"],
            });
        }

        var (display, setupCode) = QueueDisplay.Create(command.Name, branch.Id, command.ExpiresAt, now, staffId);

        displays.Add(display);

        audit.Record(new AuditEvent(
            AuditActions.QueueDisplayCreated,
            AuditResources.QueueDisplay,
            display.Id,
            BranchId: display.BranchId,
            Details: new Dictionary<string, object?>
            {
                ["name"] = display.Name,
                ["expires_at"] = display.ExpiresAt,
                ["setup_expires_at"] = display.SetupExpiresAt,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedQueueDisplayDto(
            display.Id,
            display.Name,
            display.BranchId,
            display.StateAt(now),
            setupCode,
            display.SetupExpiresAt!.Value,
            display.ExpiresAt,
            display.CreatedAt);
    }
}
