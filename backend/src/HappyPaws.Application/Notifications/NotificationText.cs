using HappyPaws.Application.Notifications.Templates;

namespace HappyPaws.Application.Notifications;

/// <summary>A message in words: what the inbox shows, and the record it is about for the inbox to link to.</summary>
public sealed record RenderedNotification(string Title, string Body, string? ReferenceId);

/// <summary>
/// The Thai wording of each message, for the web inbox.
/// <para>
/// Kept with the template keys rather than in each use case, so a use case still says "tell the
/// owner their queue was called" without writing the sentence. The LINE, SMS and email adapters,
/// when they exist, may word the same message differently for their medium; the inbox is the one
/// place that needs plain text now.
/// </para>
/// </summary>
public static class NotificationText
{
    public static RenderedNotification Render(NotificationRequest request)
    {
        string Value(string key) => request.Data.TryGetValue(key, out var value) ? value : string.Empty;

        return request.TemplateKey switch
        {
            NotificationTemplates.QueueCalled => new(
                $"ถึงคิว {Value("number")} แล้ว",
                string.IsNullOrEmpty(Value("room")) ? "เชิญที่ห้องตรวจ" : $"เชิญที่ห้องตรวจ {Value("room")}",
                null),

            NotificationTemplates.ConsultationSummary => new(
                "สรุปการตรวจ",
                string.IsNullOrEmpty(Value("diagnosis"))
                    ? "คุณหมอบันทึกผลการตรวจเรียบร้อยแล้ว"
                    : $"ผลการวินิจฉัย: {Value("diagnosis")}",
                Value("record_id")),

            NotificationTemplates.ReceiptIssued => new(
                $"ใบเสร็จ {Value("receipt_number")}",
                $"ชำระแล้ว {Value("amount")} บาท ขอบคุณที่ใช้บริการ",
                Value("invoice_id")),

            NotificationTemplates.PetDeceasedAppointmentsCancelled => new(
                "ยกเลิกนัดหมายแล้ว",
                "คลินิกขอแสดงความเสียใจ นัดหมายที่เหลือทั้งหมดถูกยกเลิกแล้ว",
                Value("pet_id")),

            NotificationTemplates.PortalInvitation => new(
                "ยินดีต้อนรับสู่ portal ของคลินิก",
                "ดูนัดหมาย ประวัติ และใบเสร็จของสัตว์เลี้ยงได้ที่นี่",
                null),

            NotificationTemplates.WaitingListSlotAvailable => new(
                "มีคิวว่างแล้ว",
                $"{Value("service_name")} สำหรับ {Value("pet_name")} ว่างวันที่ {Value("date")} เวลา {Value("time")} จองได้เลย ใครจองก่อนได้ก่อน",
                Value("waiting_list_id")),

            // The inbox copy never carries the review link: for a customer without a portal account
            // it holds a one-time token, and the inbox is a stored table (docs/contract-extensions.md § 36).
            NotificationTemplates.ReviewInvitation => new(
                $"ขอบคุณที่พา {Value("pet_name")} มาที่คลินิก",
                $"ขอบคุณที่พา {Value("pet_name")} มาใช้บริการ ให้คะแนนและเล่าให้คลินิกฟังได้ที่หน้ารีวิวในระบบออนไลน์",
                null),

            // The newer templates are worded once, in the catalogue, for every channel and the inbox.
            _ when NotificationTemplateCatalogue.Find(request.TemplateKey) is { } template => new(
                TemplateText.Fill(template.Title, request.Data),
                TemplateText.Fill(template.Line, request.Data),
                null),

            _ => new("ข้อความจากคลินิก", string.Join(" ", request.Data.Values), null),
        };
    }
}
