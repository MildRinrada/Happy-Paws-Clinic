using HappyPaws.Domain.Notifications;

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
    IReadOnlyDictionary<string, string> Data)
{
    /// <summary>The branch the message is about, for the delivery log's branch filter. Optional.</summary>
    public string? BranchId { get; init; }

    /// <summary>
    /// <c>line</c>, <c>sms</c> or <c>email</c> to use that channel only, with no fallback; null lets
    /// the router choose LINE, then SMS, then email.
    /// </summary>
    public string? Channel { get; init; }

    /// <summary>
    /// <c>line</c>, <c>sms</c> or <c>email</c> to try first, then the usual order as fallback. A thread
    /// reply uses it for "ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด". Ignored when <see cref="Channel"/> is set.
    /// </summary>
    public string? PreferredChannel { get; init; }

    public DeliverySource Source { get; init; } = DeliverySource.System;

    /// <summary>Groups the deliveries of one staff send or broadcast.</summary>
    public string? BatchId { get; init; }

    public string? SentByStaffId { get; init; }
}

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

    /// <summary>"เทมเพลตพร้อมตัวแปร pet_name, vet_name, time" — a visit coming up. Staff can send it.</summary>
    public const string AppointmentReminder = "appointment.reminder";

    /// <summary>A vaccination falling due. Staff can send it.</summary>
    public const string VaccineDue = "vaccine.due";

    /// <summary>
    /// "แผนหลายครั้ง · ผูกกับนัดและการเตือน" — a session of a treatment plan is coming up. Sent by a
    /// reminder rule, never by hand: it names a real plan.
    /// </summary>
    public const string TreatmentPlanSession = "treatment_plan.session";

    /// <summary>A message about the pet's care, written by staff. Staff can send it.</summary>
    public const string ClinicMessage = "clinic.message";

    /// <summary>
    /// "ตอบกลับ" — staff answering a customer's message in their thread (<c>POST /staff/threads/{id}/messages</c>).
    /// Not sendable from the notifications page: it belongs to a thread.
    /// </summary>
    public const string ThreadReply = "thread.reply";

    /// <summary>An announcement to a list of customers. Marketing: it needs that consent.</summary>
    public const string ClinicAnnouncement = "clinic.announcement";

    /// <summary>
    /// "คิดถึงน้อง" — a customer later back than their own usual gap, from the <c>customer_lapsed</c>
    /// reminder rule (<c>docs/contract-extensions.md § 33</c>). Marketing: it needs that consent.
    /// </summary>
    public const string CustomerLapsed = "customer.lapsed";

    /// <summary>
    /// "ช่วยรีวิวการมาครั้งนี้": a visit is over and the customer is asked to review it, from the
    /// <c>visit_completed</c> reminder rule (<c>docs/contract-extensions.md § 36</c>). Carries the
    /// review link as <c>review_url</c>. Marketing: it needs that consent.
    /// </summary>
    public const string ReviewInvitation = "review.invitation";
}
