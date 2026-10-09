using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Customers.CreateCustomer;

/// <summary>
/// <c>POST /staff/customers</c> — "สร้างจากเคาน์เตอร์ · ตรวจซ้ำด้วยเบอร์โทร".
/// </summary>
/// <param name="DisplayName">The person's name as they gave it.</param>
/// <param name="Phone">Primary contact, and the duplicate check.</param>
/// <param name="Email">Optional.</param>
/// <param name="AllowDuplicatePhone">
/// Sent only after a first attempt was refused with <c>409 duplicate_customer</c>. A household
/// really does share one number, so the check has to be a stop rather than a wall.
/// </param>
public sealed record CreateCustomerCommand(
    string DisplayName,
    string Phone,
    string? Email = null,
    bool AllowDuplicatePhone = false);

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Email).EmailAddress().When(command => !string.IsNullOrWhiteSpace(command.Email));
    }
}

/// <summary>
/// Registers a customer at the counter.
/// <para>
/// "ตรวจซ้ำด้วยเบอร์โทร" is implemented as a <b>refusal the caller can override</b>, not as a
/// unique constraint and not as a silent warning. A unique index would be wrong — a couple, a
/// parent and child, a shop and its owner all legitimately give one number, and the merge endpoint
/// exists because duplicates happen anyway. A warning nobody has to acknowledge would be ignored by
/// the third week.
/// </para>
/// <para>
/// So the first attempt answers <c>409 duplicate_customer</c> and names the existing records in
/// <c>details</c>, and the counter either picks one of them or repeats the request with
/// <c>allow_duplicate_phone</c>. It is the same shape as the allergy check on prescriptions, for the
/// same reason: the system knows something is odd and the person in the room knows whether it
/// matters.
/// </para>
/// </summary>
public sealed class CreateCustomerHandler(
    ICustomerRepository customers,
    IAuditTrail audit,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<CustomerDto> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Customer? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (!command.AllowDuplicatePhone)
            {
                await EnsurePhoneIsNewAsync(command.Phone, token);
            }

            created = Customer.Create(command.DisplayName, command.Phone, command.Email);
            customers.Add(created);
            webhooks.Enqueue(WebhookEventTypes.CustomerCreated, WebhookPayloads.Customer(created, clock.UtcNow));

            audit.Record(new AuditEvent(
                AuditActions.CustomerCreated,
                AuditResources.Customer,
                created.Id,
                Details: new Dictionary<string, object?>
                {
                    ["display_name"] = created.DisplayName,
                    ["duplicate_phone_accepted"] = command.AllowDuplicatePhone ? true : null,
                }));
        }, cancellationToken);

        return CustomerDto.From(created!);
    }

    private async Task EnsurePhoneIsNewAsync(string phone, CancellationToken cancellationToken)
    {
        var existing = await customers.WithPhoneAsync(phone, cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        throw new DuplicateCustomerException([.. existing.Select(CustomerDto.From)]);
    }
}

/// <summary>
/// <c>409 duplicate_customer</c> — the phone number is already on file.
/// <para>
/// The matching records travel in <c>details.existing</c> so the counter can pick one instead of
/// creating a second. Not in the API reference's error list; see <c>docs/assumptions.md § 14</c>.
/// </para>
/// </summary>
public sealed class DuplicateCustomerException : AppException
{
    public DuplicateCustomerException(IReadOnlyList<CustomerDto> existing)
        : base("duplicate_customer", "มีลูกค้าที่ใช้เบอร์โทรนี้อยู่แล้ว", HttpStatus.Conflict)
    {
        MessageEn = "A customer with this phone number already exists. "
            + "Resend with allow_duplicate_phone to create a second record.";
        Field = "phone";
        Details = new Dictionary<string, object> { ["existing"] = existing };
    }
}
