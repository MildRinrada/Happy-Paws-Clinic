using HappyPaws.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Integrations.HandleLineWebhook;

/// <summary><c>POST /webhooks/line</c>: the raw body as received, and LINE's <c>X-Line-Signature</c>.</summary>
public sealed record HandleLineWebhookCommand(ReadOnlyMemory<byte> Body, string? Signature);

public enum LineWebhookOutcome
{
    /// <summary>Genuine and read. Answer 200, whatever happened to each event.</summary>
    Accepted,

    /// <summary>The signature does not match the channel secret, or there is no secret to check it with.</summary>
    BadSignature,

    /// <summary>Signed, but not a body LINE sends.</summary>
    Malformed,
}

/// <summary>
/// <c>POST /webhooks/line</c> — "รับ follow · message · postback จากปุ่มในแชต".
/// <para>
/// <b>An integration callback, not a sign-in.</b> The signature proves the request came from LINE
/// for this clinic's channel; it says nothing about who the person is. Nothing here issues a token,
/// creates an account or links one. The LINE user id is looked up as a delivery address, and only
/// a customer whose record already carries it is found (<c>docs/assumptions.md § A, § B</c>).
/// </para>
/// <list type="bullet">
/// <item><c>follow</c> answers with a welcome, and if the address belongs to a customer who had
/// blocked the account, starts sending to them again.</item>
/// <item><c>unfollow</c> stops pushes to that customer at once ("หยุดส่งเข้าแชตทันที"). LINE would
/// refuse them anyway; marking it saves a failed call per message and shows in the log why.</item>
/// <item><c>message</c> answers that the account sends notifications and is not staffed.</item>
/// <item><c>postback</c> runs one of the five chat buttons through <see cref="LinePostbackExecutor"/>.</item>
/// </list>
/// <para>
/// One bad event never fails the rest. LINE retries a webhook that does not answer 200, and a retry
/// of a batch that half-worked would run the half that worked twice.
/// </para>
/// </summary>
public sealed class HandleLineWebhookHandler(
    ILineGateway line,
    ILineAddressBook addresses,
    LinePostbackExecutor postbacks,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<HandleLineWebhookHandler> logger)
{
    public async Task<LineWebhookOutcome> HandleAsync(
        HandleLineWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await line.VerifySignatureAsync(command.Body, command.Signature, cancellationToken))
        {
            return LineWebhookOutcome.BadSignature;
        }

        var events = LineWebhookPayload.Parse(command.Body);

        if (events is null)
        {
            return LineWebhookOutcome.Malformed;
        }

        foreach (var item in events)
        {
            try
            {
                await HandleEventAsync(item, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "LINE webhook event {Type} was not handled", item.Type);
            }
        }

        return LineWebhookOutcome.Accepted;
    }

    private async Task HandleEventAsync(LineWebhookEvent item, CancellationToken cancellationToken)
    {
        // Events from a group or a room have no single person behind them, and a standby event
        // belongs to another channel that is handling the chat.
        if (item.UserId is not { Length: > 0 } userId || item.Mode == "standby")
        {
            return;
        }

        switch (item.Type)
        {
            case "follow":
            {
                var customer = await addresses.FindTrackedAsync(userId, cancellationToken);
                var returning = customer?.LineUnfollowedAt is not null;

                if (returning)
                {
                    customer!.MarkLineFollowed();
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await ReplyAsync(item, new LineReply(returning ? LineReplies.WelcomeBack : LineReplies.Welcome), cancellationToken);
                break;
            }

            case "unfollow":
            {
                var customer = await addresses.FindTrackedAsync(userId, cancellationToken);

                if (customer is not null)
                {
                    customer.MarkLineUnfollowed(clock.UtcNow);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                break;
            }

            case "message":
                await ReplyAsync(item, new LineReply(LineReplies.AutoReply(line.PortalUrl)), cancellationToken);
                break;

            case "postback":
            {
                var postback = LinePostbackData.Parse(item.PostbackData);
                var reply = postback is null
                    ? new LineReply(LineReplies.NotFound)
                    : await postbacks.ExecuteAsync(await addresses.FindTrackedAsync(userId, cancellationToken), postback, cancellationToken);

                await ReplyAsync(item, reply, cancellationToken);
                break;
            }
        }
    }

    private Task ReplyAsync(LineWebhookEvent item, LineReply reply, CancellationToken cancellationToken) =>
        item.ReplyToken is { Length: > 0 } token
            ? line.ReplyAsync(token, reply.Text, reply.QuickReplies, cancellationToken)
            : Task.CompletedTask;
}
