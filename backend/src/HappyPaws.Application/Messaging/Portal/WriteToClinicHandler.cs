using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Messaging.Dtos;
using HappyPaws.Application.Messaging.ReceiveMessage;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Messaging;

namespace HappyPaws.Application.Messaging.Portal;

/// <summary><c>POST /portal/thread/messages</c> — <c>{ "text": "..." }</c>.</summary>
public sealed record WriteToClinicCommand(string? Text);

public sealed class WriteToClinicValidator : AbstractValidator<WriteToClinicCommand>
{
    public WriteToClinicValidator()
    {
        RuleFor(command => command.Text)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithMessage("ต้องพิมพ์ข้อความ")
            .Must(text => text is null || text.Trim().Length <= ThreadMessage.PortalTextMax)
            .WithMessage($"ข้อความยาวได้ไม่เกิน {ThreadMessage.PortalTextMax} ตัวอักษร");
    }
}

/// <summary>
/// The signed-in customer writes to the clinic from the portal: <b>contract extension</b>
/// (<c>docs/contract-extensions.md § 24</c>).
/// <para>
/// <b>The same inbox as LINE.</b> The message goes through <see cref="ReceiveCustomerMessageHandler"/>
/// with the channel <c>portal</c>, so it lands in the one thread the customer has whatever the
/// channel, opens that thread on the first message, refiles it under the branch they last booked
/// at, and survives two first messages racing exactly as LINE messages do.
/// </para>
/// <para>
/// No automatic acknowledgement is sent: on LINE it tells someone typing into a chat app that a
/// person will answer, which the portal page says itself. The thread now awaits a reply, and a
/// staff reply reaches the chat page, the web inbox and the usual LINE, SMS or email push.
/// </para>
/// </summary>
public sealed class WriteToClinicHandler(
    ReceiveCustomerMessageHandler receiver,
    ICustomerRepository customers,
    ICurrentUser currentUser,
    RequestValidator validator)
{
    public async Task<PortalThreadMessageDto> HandleAsync(WriteToClinicCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var customerId = currentUser.RequireCustomerId();
        var customer = await customers.FindByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        if (customer.IsMerged)
        {
            // A token minted before a merge: the thread belongs to the surviving record now.
            throw new StateConflictException("Customer", "merged", "write");
        }

        var received = await receiver.ReceiveAsync(
            new CustomerMessage(customerId, MessageChannel.Portal, MessageKind.Text, command.Text!.Trim(), null),
            cancellationToken);

        return PortalThreadMessageDto.From(received.Message!, null);
    }
}
