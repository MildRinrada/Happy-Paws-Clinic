using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>
/// <c>POST /staff/pets/{id}/documents</c> — "อัปโหลดผ่าน pre-signed URL · เลือกให้เจ้าของเห็นได้".
/// <c>VisibleToOwner</c> defaults to false: a clinic document reaches the owner only when someone
/// decides it should.
/// </summary>
public sealed record CreateDocumentUploadUrlCommand(
    string PetId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Kind = null,
    string? Title = null,
    bool VisibleToOwner = false,
    string? ConsultationId = null);

public sealed class CreateDocumentUploadUrlValidator : AbstractValidator<CreateDocumentUploadUrlCommand>
{
    public CreateDocumentUploadUrlValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.SizeBytes).GreaterThan(0);
        RuleFor(command => command.Title).MaximumLength(200);
    }
}

/// <summary>
/// Records a document on a patient's file and hands back the URL to upload it to.
/// <para>
/// The bytes never pass through this request. The answer is a short-lived URL that accepts one file
/// of the declared type and at most the declared size; the document becomes downloadable when that
/// file arrives.
/// </para>
/// </summary>
public sealed class CreateDocumentUploadUrlHandler(
    IPetRepository pets,
    IConsultationRepository consultations,
    DocumentUploads uploads,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<DocumentUploadDto> HandleAsync(
        CreateDocumentUploadUrlCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        DocumentUploadDto? started = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            if (command.ConsultationId is { Length: > 0 } consultationId)
            {
                var consultation = await consultations.FindByIdAsync(consultationId, token);
                if (consultation is null || consultation.PetId != pet.Id)
                {
                    throw new NotFoundException("consultation", consultationId);
                }
            }

            started = uploads.Start(PetDocument.Start(
                pet.Id,
                DocumentSource.Clinic,
                staffId,
                command.FileName,
                command.ContentType,
                command.SizeBytes,
                command.Kind,
                command.Title,
                command.VisibleToOwner,
                command.ConsultationId));
        }, cancellationToken);

        return started!;
    }
}
