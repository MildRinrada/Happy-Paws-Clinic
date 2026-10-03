using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.ManageContacts;

// The clinic's contact entries — a contract extension; see docs/contract-extensions.md § 15.
// Read publicly by the website, written under settings:write like the rest of clinic setup.

/// <summary><c>GET /clinic/contacts</c> — every entry, in display order.</summary>
public sealed class ListContactsHandler(IContactRepository contacts)
{
    public async Task<IReadOnlyList<ContactDto>> HandleAsync(CancellationToken cancellationToken = default) =>
        [.. (await contacts.ListAsync(cancellationToken)).Select(ContactDto.From)];
}

/// <summary><c>POST /clinic/contacts</c>.</summary>
public sealed record AddContactCommand(
    string Kind,
    string Label,
    string Value,
    string? Url = null,
    string? EmbedUrl = null,
    string? BranchId = null,
    int SortOrder = 0);

public sealed class AddContactValidator : AbstractValidator<AddContactCommand>
{
    public AddContactValidator()
    {
        ContactRules.Kind(RuleFor(command => command.Kind));
        ContactRules.Label(RuleFor(command => command.Label));
        ContactRules.Value(RuleFor(command => command.Value));
        ContactRules.Url(RuleFor(command => command.Url));
        ContactRules.EmbedUrl(RuleFor(command => command.EmbedUrl));
        RuleFor(command => command.EmbedUrl)
            .Must((command, embed) => string.IsNullOrWhiteSpace(embed) || command.Kind?.Trim().ToLowerInvariant() == ContactKinds.Map)
            .WithMessage("ลิงก์ฝังแผนที่ใช้ได้กับประเภทแผนที่เท่านั้น");
    }
}

public sealed class AddContactHandler(
    IContactRepository contacts,
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<ContactDto> HandleAsync(AddContactCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        ClinicContact? added = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await ContactBranch.EnsureOpenAsync(branches, command.BranchId, token);

            added = ClinicContact.Add(
                command.Kind, command.Label, command.Value, command.Url, command.EmbedUrl, command.BranchId, command.SortOrder);

            contacts.Add(added);
        }, cancellationToken);

        return ContactDto.From(added!);
    }
}

/// <summary>
/// <c>PATCH /clinic/contacts/{id}</c>. An absent field is left as it is; an empty string clears the
/// optional ones (<c>url</c>, <c>embed_url</c>, <c>branch_id</c>), the same rule as <c>PATCH /clinic</c>.
/// </summary>
public sealed record UpdateContactCommand(
    string ContactId,
    string? Kind = null,
    string? Label = null,
    string? Value = null,
    string? Url = null,
    string? EmbedUrl = null,
    string? BranchId = null,
    int? SortOrder = null,
    uint? IfMatchVersion = null);

public sealed class UpdateContactValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactValidator()
    {
        When(command => command.Kind is not null, () => ContactRules.Kind(RuleFor(command => command.Kind)));
        When(command => command.Label is not null, () => ContactRules.Label(RuleFor(command => command.Label)));
        When(command => command.Value is not null, () => ContactRules.Value(RuleFor(command => command.Value)));
        ContactRules.Url(RuleFor(command => command.Url));
        ContactRules.EmbedUrl(RuleFor(command => command.EmbedUrl));
    }
}

public sealed class UpdateContactHandler(
    IContactRepository contacts,
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<ContactDto> HandleAsync(UpdateContactCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        ClinicContact? changed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var contact = await contacts.FindTrackedAsync(command.ContactId, token)
                ?? throw new NotFoundException("contact", command.ContactId);

            ConcurrencyGuard.Ensure(contact, command.IfMatchVersion);

            var branchId = command.BranchId ?? contact.BranchId;
            await ContactBranch.EnsureOpenAsync(branches, branchId, token);

            try
            {
                contact.Change(
                    command.Kind ?? contact.Kind,
                    command.Label ?? contact.Label,
                    command.Value ?? contact.Value,
                    command.Url ?? contact.Url,
                    command.EmbedUrl ?? contact.EmbedUrl,
                    branchId,
                    command.SortOrder ?? contact.SortOrder);
            }
            catch (Domain.Common.DomainException refused)
            {
                // The one rule the fields cannot check alone: an embed left on an entry whose kind
                // is being changed away from a map. Reported on the field like any other.
                throw new ValidationFailedException(
                    new Dictionary<string, string[]> { ["embed_url"] = [refused.Message] });
            }

            changed = contact;
        }, cancellationToken);

        return ContactDto.From(changed!);
    }
}

/// <summary><c>DELETE /clinic/contacts/{id}</c>. Removing an entry already gone answers 404.</summary>
public sealed record RemoveContactCommand(string ContactId, uint? IfMatchVersion = null);

public sealed class RemoveContactHandler(IContactRepository contacts, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RemoveContactCommand command, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var contact = await contacts.FindTrackedAsync(command.ContactId, token)
                ?? throw new NotFoundException("contact", command.ContactId);

            ConcurrencyGuard.Ensure(contact, command.IfMatchVersion);

            contacts.Remove(contact);
        }, cancellationToken);
    }
}

/// <summary>The branch an entry names must be one of the clinic's open branches.</summary>
internal static class ContactBranch
{
    public static async Task EnsureOpenAsync(IBranchRepository branches, string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 } id && (await branches.MissingAsync([id], cancellationToken)).Count > 0)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["branch_id"] = ["ไม่พบสาขาที่ระบุ"] });
        }
    }
}
