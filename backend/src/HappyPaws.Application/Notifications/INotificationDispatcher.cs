namespace HappyPaws.Application.Notifications;

/// <summary>One message to send to a customer, on whichever channel their record allows.</summary>
/// <param name="CustomerId">Who to reach. The adapter resolves the channel from the customer record.</param>
/// <param name="TemplateKey">
/// Which message to send — <c>queue.called</c>, <c>appointment.reminder</c>. A key rather than
/// prose, so the wording lives with the channel adapter and can be translated or restyled without
/// touching a use case.
/// </param>
/// <param name="Data">Template substitutions: the queue number, the room, the appointment time.</param>
public sealed record NotificationRequest(
    string CustomerId,
    string TemplateKey,
    IReadOnlyDictionary<string, string> Data);

/// <summary>
/// Sends a customer a message. The only thing Application code knows about notifications.
/// <para>
/// LINE, SMS and email sit behind this one port, so a use case says "tell the owner their queue was
/// called" without knowing or caring which channel carries it. A customer with no LINE address
/// simply falls back to another channel, or to none — see
/// <c>docs/assumptions.md § A</c> on why LINE is outbound only.
/// </para>
/// <para>
/// <b>Never called inside a database transaction.</b> A slow gateway would hold row locks for its
/// whole timeout, and a failed push must not roll back a clinical write. Use cases dispatch after
/// the transaction commits, accepting that a message can be lost if the process dies in between —
/// which is the right trade against a queue call that silently fails because LINE was down.
/// </para>
/// </summary>
public interface INotificationDispatcher
{
    Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>The template keys in use. Kept together so the channel adapters have one list to cover.</summary>
public static class NotificationTemplates
{
    /// <summary>"เรียกคิว · ยิงแจ้งเตือน LINE ให้เจ้าของ" — the queue call push.</summary>
    public const string QueueCalled = "queue.called";

    /// <summary>
    /// "LINE: สรุปการตรวจ + ใบเสร็จ" — sent when a case is closed and its record signed.
    /// </summary>
    public const string ConsultationSummary = "consultation.summary";

    /// <summary>
    /// Sent once when a patient record is closed and its future visits are cancelled with it. The
    /// last message this clinic will send about that patient — see
    /// <c>MarkPetDeceasedHandler</c>.
    /// </summary>
    public const string PetDeceasedAppointmentsCancelled = "pet.deceased_appointments_cancelled";

    /// <summary>
    /// "ออกใบเสร็จ/ใบกำกับภาษี · ส่งเข้า LINE" — the customer's copy of what they just paid.
    /// </summary>
    public const string ReceiptIssued = "receipt.issued";

    /// <summary>
    /// "ชวนให้เปิดใช้ portal" — the counter asks a customer to open a portal account.
    /// </summary>
    public const string PortalInvitation = "customer.portal_invitation";

    /// <summary>
    /// "ยิง LINE อัตโนมัติเมื่อมีคนยกเลิก" — a slot the customer is waiting for has opened up.
    /// </summary>
    public const string WaitingListSlotAvailable = "waiting_list.slot_available";
}
