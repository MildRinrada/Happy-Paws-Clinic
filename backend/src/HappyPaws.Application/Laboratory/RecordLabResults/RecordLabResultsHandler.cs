using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.RecordLabResults;

/// <summary><c>POST /staff/lab-requests/{id}/results</c> — "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ".</summary>
public sealed record RecordLabResultsCommand(string LabRequestId, IReadOnlyList<LabResultInput> Results);

public sealed class RecordLabResultsValidator : AbstractValidator<RecordLabResultsCommand>
{
    public RecordLabResultsValidator()
    {
        RuleFor(command => command.Results).NotEmpty();
        RuleForEach(command => command.Results).ChildRules(result =>
        {
            result.RuleFor(item => item.Analyte).NotEmpty().MaximumLength(100);
            result.RuleFor(item => item.ValueText).MaximumLength(500);
            result.RuleFor(item => item.Unit).MaximumLength(30);
            result.RuleFor(item => item.ReferenceText).MaximumLength(200);
        });
    }
}

/// <summary>
/// Enters measured values against a request.
/// <para>
/// The flag is worked out from the value and its range, so "ธงค่าผิดปกติ" does not depend on the
/// person typing remembering to tick a box. New results put the request back on the "not yet read"
/// list, including one a vet had already read: a value added after review is a value nobody has
/// seen.
/// </para>
/// </summary>
public sealed class RecordLabResultsHandler(
    LabRequestAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(
        RecordLabResultsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        LabRequest? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await access.ForStaffAsync(command.LabRequestId, token);
            var added = request.RecordResults(command.Results, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.LabResultsRecorded,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?>
                {
                    ["results"] = added.Count,
                    ["abnormal"] = added.Count(result => result.IsAbnormal),
                }));

            updated = request;
        }, cancellationToken);

        return LabRequestDto.From(updated!);
    }
}
