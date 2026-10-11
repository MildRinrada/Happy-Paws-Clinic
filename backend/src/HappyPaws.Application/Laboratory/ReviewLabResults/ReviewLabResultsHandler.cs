using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.ReviewLabResults;

/// <summary>
/// <c>POST /staff/lab-requests/{id}/review</c> — a contract extension. See
/// <c>docs/contract-extensions.md § 11</c>; the optional <c>Note</c> is § 53.
/// </summary>
public sealed record ReviewLabResultsCommand(string LabRequestId, string? Note = null);

public sealed class ReviewLabResultsValidator : AbstractValidator<ReviewLabResultsCommand>
{
    public ReviewLabResultsValidator()
    {
        RuleFor(command => command.Note).MaximumLength(LabRequest.MaxReviewNoteLength);
    }
}

/// <summary>
/// Records that a vet has read a set of results.
/// <para>
/// The worklist the contract describes has a second half, "ผลที่ยังไม่มีคนอ่าน", and nothing in
/// the contract can take a result off it. Opening a list is not reading a result, so this is the
/// act that does: it names who read it and when, which is also what a complaint about a missed
/// abnormal value will ask.
/// </para>
/// <para>
/// The vet may leave a short note with the reading (§ 53): what they made of it and what happens
/// next. It is stored on the request, shown wherever the results are, and written to the audit row.
/// </para>
/// </summary>
public sealed class ReviewLabResultsHandler(
    LabRequestAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(
        ReviewLabResultsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        LabRequest? reviewed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await access.ForStaffAsync(command.LabRequestId, token);
            var alreadyReviewed = request.Status == LabRequestStatus.Reviewed;

            request.Review(staffId, clock.UtcNow, command.Note);

            if (!alreadyReviewed)
            {
                audit.Record(new AuditEvent(
                    AuditActions.LabResultsReviewed,
                    AuditResources.LabRequest,
                    request.Id,
                    request.PetId,
                    request.BranchId,
                    new Dictionary<string, object?>
                    {
                        ["abnormal"] = request.AbnormalCount,
                        ["note"] = request.ReviewNote,
                    }));
            }

            reviewed = request;
        }, cancellationToken);

        return LabRequestDto.From(reviewed!);
    }
}
