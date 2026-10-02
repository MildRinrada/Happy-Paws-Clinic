using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>
/// <c>POST /portal/documents</c> — "อัปโหลดรูปอาการหรือผลจากคลินิกอื่น". <c>Kind</c> is
/// <c>photo</c>, <c>lab_result</c>, <c>referral</c>, <c>imaging</c> or <c>other</c>.
/// </summary>
public sealed record UploadOwnerDocumentCommand(
    string PetId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Kind = null,
    string? Title = null);

public sealed class UploadOwnerDocumentValidator : AbstractValidator<UploadOwnerDocumentCommand>
{
    public UploadOwnerDocumentValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.SizeBytes).GreaterThan(0);
        RuleFor(command => command.Title).MaximumLength(200);
    }
}

/// <summary>
/// An owner adds a file to their own pet's record — a photo of a symptom, results from another
/// clinic.
/// <para>
/// Only for a pet the caller cares for; anyone else's pet is <c>404</c>. The document is marked as
/// the owner's, so the clinic can tell what it wrote from what it was sent: a lab result typed by a
/// vet and a lab result photographed by an owner carry different weight.
/// </para>
/// </summary>
public sealed class UploadOwnerDocumentHandler(
    IPetRepository pets,
    PetAccessGuard petAccess,
    DocumentUploads uploads,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<DocumentUploadDto> HandleAsync(
        UploadOwnerDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var customerId = currentUser.RequireCustomerId();
        await petAccess.EnsureCaresForAsync(command.PetId, cancellationToken);

        DocumentUploadDto? started = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            started = uploads.Start(PetDocument.Start(
                pet.Id,
                DocumentSource.Owner,
                customerId,
                command.FileName,
                command.ContentType,
                command.SizeBytes,
                command.Kind,
                command.Title));
        }, cancellationToken);

        return started!;
    }
}
