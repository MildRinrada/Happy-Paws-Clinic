namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>
/// Queues an outbound webhook event as part of the current unit of work.
/// <para>
/// <b>Call it inside the transaction that makes the change</b>, before it commits (inside the
/// <c>ExecuteInTransactionAsync</c> callback, or before <c>SaveChangesAsync</c>). It only adds a
/// row to the context; it never sends. Rolled back, the event is gone with the change. Committed,
/// it is on disk and the webhook job will deliver it. That is the whole point: nothing is sent for
/// a change that did not happen, and nothing is lost for one that did.
/// </para>
/// <para>
/// The data is one of the records in <see cref="WebhookPayloads"/>, never an
/// entity or a DTO, so what leaves the system is decided in one reviewed file.
/// </para>
/// </summary>
public interface IWebhookOutbox
{
    void Enqueue(string eventType, object data);
}
