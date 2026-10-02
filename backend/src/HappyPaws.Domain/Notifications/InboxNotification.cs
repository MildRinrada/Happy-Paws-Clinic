using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Notifications;

/// <summary>
/// A message as it sits in the customer's inbox on the web — <c>GET /portal/notifications</c>,
/// "กล่องแจ้งเตือนในเว็บ · mark as read".
/// <para>
/// Every message the clinic sends a customer is also put here, after the consent check and before
/// any channel. So the inbox is the record of what the clinic said to this person, whichever
/// channel carried it or failed to: a LINE push that never arrived is still in the inbox.
/// </para>
/// </summary>
public sealed class InboxNotification : Entity
{
    private InboxNotification()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>Which message — <c>queue.called</c>, <c>receipt.issued</c>.</summary>
    public string TemplateKey { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    /// <summary>What the message is about, for a link: an appointment, a receipt, a queue ticket.</summary>
    public string? ReferenceId { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static InboxNotification Deliver(
        string customerId,
        string templateKey,
        string title,
        string body,
        string? referenceId = null) =>
        new()
        {
            Id = EntityId.New(EntityId.Notification),
            CustomerId = customerId,
            TemplateKey = templateKey,
            Title = title,
            Body = body,
            ReferenceId = referenceId,
        };

    public void MarkRead(DateTimeOffset at) => ReadAt ??= at;
}
