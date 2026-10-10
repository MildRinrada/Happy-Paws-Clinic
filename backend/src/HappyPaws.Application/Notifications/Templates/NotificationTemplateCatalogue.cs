namespace HappyPaws.Application.Notifications.Templates;

/// <summary>
/// Every message the clinic sends, in Thai, per channel — what <c>GET /staff/notifications/templates</c>
/// lists ("เทมเพลตพร้อมตัวแปร pet_name, vet_name, time").
/// <para>
/// Kept in code rather than in a table. The contract gives templates a read and no write, so a
/// table would be a second copy of this file that nothing can change; when an endpoint for editing
/// wording is agreed, this list becomes the seed for it.
/// </para>
/// <para>
/// <b>Health data stays off the wire.</b> The visit summary does not put the diagnosis in a LINE
/// push, an SMS or an email: those pass through LINE's, the SMS gateway's and the mail provider's
/// servers. The message says the record is ready and the owner reads it in the portal. The web
/// inbox, which is the clinic's own system, keeps showing it (<see cref="NotificationText"/>).
/// </para>
/// </summary>
public static class NotificationTemplateCatalogue
{
    public static readonly IReadOnlyList<NotificationTemplate> All =
    [
        new(
            NotificationTemplates.AppointmentReminder,
            "เตือนนัดหมาย",
            StaffSendable: true,
            [new("pet_name"), new("date", MaxLength: 40), new("time", MaxLength: 20), new("vet_name", Required: false)],
            "เตือนนัดหมาย",
            "เตือนนัดหมาย {pet_name} วันที่ {date} เวลา {time} น.[ กับ{vet_name}] หากมาไม่ได้กรุณาแจ้งคลินิกล่วงหน้า",
            "{clinic}: นัด{pet_name} {date} {time}น.[ {vet_name}]"),

        new(
            NotificationTemplates.VaccineDue,
            "ครบกำหนดวัคซีน",
            StaffSendable: true,
            [new("pet_name"), new("date", MaxLength: 40), new("vaccine_name", Required: false)],
            "ครบกำหนดฉีดวัคซีน",
            "{pet_name} ครบกำหนดฉีดวัคซีน[ {vaccine_name}] วันที่ {date} นัดเวลาฉีดกับคลินิกได้เลย",
            "{clinic}: {pet_name} ครบกำหนดวัคซีน {date}"),

        new(
            NotificationTemplates.TreatmentPlanSession,
            "เตือนรอบแผนการรักษา",
            StaffSendable: false,
            [new("pet_name"), new("plan_title"), new("session_number", MaxLength: 3), new("date", MaxLength: 40), new("time", Required: false, MaxLength: 20)],
            "ถึงรอบ{plan_title}",
            "{pet_name} มีรอบ{plan_title} ครั้งที่ {session_number} วันที่ {date}[ เวลา {time} น.] หากยังไม่ได้นัดหรือมาไม่ได้ กรุณาติดต่อคลินิก",
            "{clinic}: {pet_name} รอบรักษาครั้งที่ {session_number} {date}"),

        new(
            NotificationTemplates.ClinicMessage,
            "ข้อความถึงเจ้าของ",
            StaffSendable: true,
            [new("message", MaxLength: 500)],
            "ข้อความจากคลินิก",
            "{message}",
            "{clinic}: {message}"),

        new(
            NotificationTemplates.ThreadReply,
            "ตอบข้อความลูกค้า",
            StaffSendable: false,
            [new("message", MaxLength: 1_000)],
            "ข้อความตอบกลับจากคลินิก",
            "{message}",
            "{clinic}: {message}"),

        new(
            NotificationTemplates.QueueCalled,
            "เรียกคิว",
            StaffSendable: false,
            [new("number"), new("room", Required: false)],
            "ถึงคิว {number} แล้ว",
            "ถึงคิว {number} แล้ว เชิญที่ห้องตรวจ[ {room}]",
            "{clinic}: ถึงคิว {number} แล้ว เชิญที่ห้องตรวจ[ {room}]"),

        new(
            NotificationTemplates.ReceiptIssued,
            "ใบเสร็จ",
            StaffSendable: false,
            [new("receipt_number"), new("amount"), new("invoice_id", Required: false)],
            "ใบเสร็จ {receipt_number}",
            "ได้รับชำระ {amount} บาท ใบเสร็จเลขที่ {receipt_number} ขอบคุณที่ใช้บริการ",
            "{clinic}: ชำระ {amount} บาท ใบเสร็จ {receipt_number}"),

        new(
            NotificationTemplates.ConsultationSummary,
            "สรุปการตรวจ",
            StaffSendable: false,
            [new("record_id", Required: false)],
            "สรุปการตรวจ",
            "คุณหมอบันทึกผลการตรวจเรียบร้อยแล้ว ดูรายละเอียดได้ที่[ {portal_url}]",
            "{clinic}: บันทึกผลการตรวจแล้ว ดูได้ในระบบออนไลน์"),

        new(
            NotificationTemplates.WaitingListSlotAvailable,
            "มีคิวว่าง",
            StaffSendable: false,
            [new("pet_name"), new("service_name"), new("date"), new("time")],
            "มีคิวว่างแล้ว",
            "{service_name} สำหรับ {pet_name} ว่างวันที่ {date} เวลา {time} น. จองได้เลย ใครจองก่อนได้ก่อน",
            "{clinic}: คิวว่าง {date} {time}น. จองได้เลย"),

        new(
            NotificationTemplates.PetDeceasedAppointmentsCancelled,
            "ยกเลิกนัดของสัตว์ที่เสียชีวิต",
            StaffSendable: false,
            [],
            "ยกเลิกนัดหมายแล้ว",
            "คลินิกขอแสดงความเสียใจ นัดหมายที่เหลือทั้งหมดถูกยกเลิกแล้ว",
            "{clinic}: ขอแสดงความเสียใจ นัดที่เหลือถูกยกเลิกแล้ว"),

        new(
            NotificationTemplates.PortalInvitation,
            "ชวนเปิดบัญชีออนไลน์",
            StaffSendable: false,
            [new("display_name", Required: false)],
            "ยินดีต้อนรับสู่ระบบออนไลน์ของคลินิก",
            "ดูนัดหมาย ประวัติ และใบเสร็จของสัตว์เลี้ยงได้ที่[ {portal_url}] สมัครด้วยเบอร์โทรที่ให้ไว้กับคลินิก",
            "{clinic}: เปิดบัญชีออนไลน์ด้วยเบอร์นี้ ดูนัดและประวัติสัตว์เลี้ยง"),

        new(
            NotificationTemplates.CustomerLapsed,
            "คิดถึงน้อง (ลูกค้าที่หายไป)",
            StaffSendable: false,
            [new("pet_name", MaxLength: 200), new("date", Required: false, MaxLength: 40)],
            "คลินิกคิดถึง {pet_name}",
            "ไม่ได้เจอ {pet_name} มาสักพักแล้ว ถ้าถึงเวลาตรวจสุขภาพหรือมีเรื่องอยากปรึกษา นัดกับคลินิกได้เลย",
            "{clinic}: คิดถึง{pet_name} นัดตรวจสุขภาพได้เลย"),

        // The review link is a one-time token for a customer without a portal account. It goes on
        // LINE, SMS and email only: the web inbox words this message without it (NotificationText),
        // so the token is never stored anywhere (docs/contract-extensions.md § 36).
        new(
            NotificationTemplates.ReviewInvitation,
            "ชวนรีวิวหลังมาใช้บริการ",
            StaffSendable: false,
            [new("pet_name", MaxLength: 200), new("date", Required: false, MaxLength: 40), new("review_url", Required: false, MaxLength: 300)],
            "ขอบคุณที่พา {pet_name} มาที่คลินิก",
            "ขอบคุณที่พา {pet_name} มาใช้บริการ[ เมื่อ {date}] ช่วยให้คะแนนและเล่าให้คลินิกฟังได้ที่[ {review_url}] รีวิวขึ้นหน้าเว็บหลังคลินิกอ่านแล้วเท่านั้น",
            "{clinic}: ขอบคุณที่มาใช้บริการ รีวิวได้ที่[ {review_url}]"),

        new(
            NotificationTemplates.ClinicAnnouncement,
            "ประกาศ (LINE)",
            StaffSendable: false,
            [new("message", MaxLength: 1_000)],
            "ประกาศจากคลินิก",
            "{message}",
            "{clinic}: {message}"),
    ];

    private static readonly Dictionary<string, NotificationTemplate> ByKey =
        All.ToDictionary(template => template.Key, StringComparer.Ordinal);

    public static NotificationTemplate? Find(string key) => ByKey.GetValueOrDefault(key);

    public static bool IsKnown(string key) => ByKey.ContainsKey(key);
}
