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
/// <c>POST /staff/lab-requests/{id}/results/{resultId}/amend</c> — <b>not in the API reference</b>
/// (<c>docs/contract-extensions.md § 52</c>). <c>Corrected</c> is the whole value as it should
/// have been; an empty analyte keeps the name it has.
/// </summary>
public sealed record AmendLabResultCommand(
    string LabRequestId,
    string ResultId,
    LabResultInput Corrected,
    string? Reason,
    uint? IfMatchVersion = null);

public sealed class AmendLabResultValidator : AbstractValidator<AmendLabResultCommand>
{
    public AmendLabResultValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("ต้องระบุเหตุผลที่แก้ผล")
            .MaximumLength(LabResult.MaxReasonLength);

        RuleFor(command => command.Corrected.Analyte).MaximumLength(100).OverridePropertyName("analyte");
        RuleFor(command => command.Corrected.ValueText).MaximumLength(500).OverridePropertyName("value_text");
        RuleFor(command => command.Corrected.Unit).MaximumLength(30).OverridePropertyName("unit");
        RuleFor(command => command.Corrected.ReferenceText).MaximumLength(200).OverridePropertyName("reference_text");
    }
}

/// <summary>
/// Corrects one value: a typing mistake, or a corrected report from the lab.
/// <para>
/// Nothing is lost. The value replaced stays on the result with who replaced it, when and why, and
/// the audit row carries both values. The flag and the abnormal count follow the new value. If a
/// vet had already read the results, the request goes back to "รออ่านผล": the value they read is
/// not the value on file any more, and a correction nobody reads is the error nobody catches.
/// </para>
/// <para>
/// <c>lab:write</c>, the scope that entered the value, because correcting it is entering it again.
/// A request at another branch is <c>404</c>, as everywhere in the lab module.
/// </para>
/// </summary>
public sealed class AmendLabResultHandler(
    LabRequestAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(AmendLabResultCommand command, CancellationToken cancellationToken = default)
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

            var before = request.AmendResult(result, command.Corrected, command.Reason!, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.LabResultAmended,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?>
                {
                    ["result_id"] = result.Id,
                    ["before"] = LabValueSnapshot.Of(before),
                    ["after"] = LabValueSnapshot.Of(result),
                    ["reason"] = result.AmendReason,
                    ["reopened"] = wasReviewed,
                }));

            updated = request;
        }, cancellationToken);

        return LabRequestDto.From(updated!);
    }
}
