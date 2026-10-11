using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.CorrectLabResults;

/// <summary>
/// <c>POST /staff/lab-requests/{id}/results/{resultId}/void</c> — <b>not in the API reference</b>
/// (<c>docs/contract-extensions.md § 52</c>).
/// </summary>
public sealed record VoidLabResultCommand(string LabRequestId, string ResultId, string? Reason, uint? IfMatchVersion = null);

public sealed class VoidLabResultValidator : AbstractValidator<VoidLabResultCommand>
{
    public VoidLabResultValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("ต้องระบุเหตุผลที่ยกเลิกค่านี้")
            .MaximumLength(LabResult.MaxReasonLength);
    }
}

/// <summary>
/// Strikes one value out, for a value that should never have been entered: the wrong patient's
/// sample, the wrong request. It stays listed, marked, and counts towards nothing. Read results go
/// back in front of a vet, as with a correction. Same scope and branch rule as
/// <see cref="AmendLabResultHandler"/>.
/// </summary>
public sealed class VoidLabResultHandler(
    LabRequestAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(VoidLabResultCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        LabRequest? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await access.ForStaffAsync(command.LabRequestId, token);
            ConcurrencyGuard.Ensure(request, command.IfMatchVersion);

            var result = request.Results.FirstOrDefault(item => item.Id == command.ResultId)
                ?? throw new NotFoundException("lab_result", command.ResultId);
            var wasReviewed = request.Status == LabRequestStatus.Reviewed;

            request.VoidResult(result, command.Reason!, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.LabResultVoided,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?>
                {
                    ["result_id"] = result.Id,
                    ["before"] = LabValueSnapshot.Of(result),
                    ["reason"] = result.VoidReason,
                    ["reopened"] = wasReviewed,
                }));

            updated = request;
        }, cancellationToken);

        return LabRequestDto.From(updated!);
    }
}
