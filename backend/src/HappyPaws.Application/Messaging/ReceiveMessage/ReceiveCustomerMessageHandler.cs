using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Messaging;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Messaging.ReceiveMessage;

/// <summary>A message a known customer wrote to the clinic, as a channel's webhook (or the portal) delivered it.</summary>
/// <param name="CustomerId">The customer whose record carries the address it came from, or the signed-in customer.</param>
/// <param name="Channel">Where it came from.</param>
/// <param name="Kind">Text, a picture, a sticker...</param>
/// <param name="Text">The words, for text; a file's name or a location's address otherwise, when there is one.</param>
/// <param name="ExternalId">The channel's id for the message, so a redelivered webhook is stored once.</param>
public sealed record CustomerMessage(
    string CustomerId,
    MessageChannel Channel,
    MessageKind Kind,
    string? Text,
    string? ExternalId);

public enum CustomerMessageOutcome
{
    /// <summary>Stored, and the thread now awaits a reply where it did not before (or is new).</summary>
    Opened,

    /// <summary>Stored in a thread that was already awaiting a reply.</summary>
    Appended,

    /// <summary>Already stored: the channel delivered it again.</summary>
    Duplicate,
}

/// <summary>What <see cref="ReceiveCustomerMessageHandler.ReceiveAsync"/> did, and the stored message (null for a duplicate).</summary>
public sealed record ReceivedCustomerMessage(CustomerMessageOutcome Outcome, ThreadMessage? Message);

/// <summary>
/// Puts a customer's message into their thread, opening the thread on the first one. Not an
/// endpoint: the LINE webhook calls it for a <c>message</c> event from an address on a customer's
/// record, which is how the contract's "รับ follow · message · postback" reaches the inbox, and
/// <c>POST /portal/thread/messages</c> calls it for the signed-in customer
/// (<c>docs/contract-extensions.md § 24</c>).
/// <para>
/// <b>Concurrent deliveries.</b> Two messages typed in quick succession can arrive in two webhook
/// calls at once. The first message of a thread then races to create it (the unique customer id
/// refuses the second), and two appends race on the thread's version. Either loser simply runs again
/// and finds the thread the winner made, so nothing the customer wrote is lost to a race.
/// </para>
/// </summary>
public sealed class ReceiveCustomerMessageHandler(
    IMessageThreadRepository threads,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ReceiveCustomerMessageHandler> logger)
{
    private const int Attempts = 3;

    public async Task<CustomerMessageOutcome> HandleAsync(CustomerMessage message, CancellationToken cancellationToken = default) =>
        (await ReceiveAsync(message, cancellationToken)).Outcome;

    /// <summary>As <see cref="HandleAsync"/>, with the stored message, for a caller that answers with it.</summary>
    public async Task<ReceivedCustomerMessage> ReceiveAsync(CustomerMessage message, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await StoreAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException && attempt < Attempts)
            {
                logger.LogWarning(exception, "Message from customer {CustomerId} collided with another; trying again", message.CustomerId);
            }
        }
    }

    private async Task<ReceivedCustomerMessage> StoreAsync(CustomerMessage message, CancellationToken cancellationToken)
    {
        var outcome = CustomerMessageOutcome.Duplicate;
        ThreadMessage? stored = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (message.ExternalId is { Length: > 0 } externalId
                && await threads.HasInboundAsync(message.Channel, externalId, token))
            {
                outcome = CustomerMessageOutcome.Duplicate;
                return;
            }

            var now = clock.UtcNow;
            var branchId = await threads.LatestBranchOfCustomerAsync(message.CustomerId, token);
            var thread = await threads.FindTrackedForCustomerAsync(message.CustomerId, token);
            var wasAwaiting = thread?.AwaitingReply ?? false;

            if (thread is null)
            {
                thread = MessageThread.Open(message.CustomerId, branchId, now);
                threads.Add(thread);
            }

            stored = thread.Receive(message.Channel, message.Kind, message.Text, message.ExternalId, branchId, now);
            threads.AddMessage(stored);
            outcome = wasAwaiting ? CustomerMessageOutcome.Appended : CustomerMessageOutcome.Opened;
        }, cancellationToken);

        return new ReceivedCustomerMessage(outcome, outcome == CustomerMessageOutcome.Duplicate ? null : stored);
    }
}
