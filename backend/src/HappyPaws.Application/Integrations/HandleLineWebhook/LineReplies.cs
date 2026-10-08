namespace HappyPaws.Application.Integrations.HandleLineWebhook;

/// <summary>
/// What the clinic's LINE account says back in the chat, in Thai.
/// <para>
/// The account sends notifications; it is not staffed. The automatic answer to a typed message
/// says so plainly, so an owner does not wait for a reply nobody will write.
/// </para>
/// </summary>
internal static class LineReplies
{
    public const string Welcome =
        "ขอบคุณที่เพิ่มเพื่อนกับคลินิก บัญชีนี้ใช้ส่งการแจ้งเตือน เช่น เรียกคิว เตือนนัด และใบเสร็จ";

    public const string WelcomeBack = "ยินดีต้อนรับกลับมา คลินิกจะส่งการแจ้งเตือนถึงคุณทางนี้อีกครั้ง";

    public static string AutoReply(string portalUrl) =>
        $"บัญชีนี้ใช้ส่งการแจ้งเตือนเท่านั้น ยังตอบแชตไม่ได้ ดูนัดหมายและติดต่อคลินิกได้ที่ {portalUrl}";

    public static string NotRegistered(string portalUrl) =>
        $"คลินิกยังไม่ได้บันทึก LINE นี้ไว้กับข้อมูลลูกค้า จึงทำรายการจากแชตไม่ได้ ทำรายการได้ที่ {portalUrl} หรือติดต่อคลินิก";

    public const string NotFound = "ไม่พบรายการนี้ หรือรายการนี้ไม่ใช่ของคุณ";

    public const string Confirmed = "ยืนยันนัดเรียบร้อยแล้ว แล้วพบกันที่คลินิก";

    public const string CannotConfirm = "ยืนยันนัดนี้ไม่ได้ นัดอาจถูกยกเลิกหรือผ่านไปแล้ว";

    public const string AskCancel = "ต้องการยกเลิกนัดนี้ใช่ไหม กดปุ่มด้านล่างเพื่อยืนยัน";

    public const string ConfirmCancelLabel = "ยืนยันยกเลิกนัด";

    public const string Cancelled = "ยกเลิกนัดเรียบร้อยแล้ว ถ้าต้องการนัดใหม่ จองได้ที่ระบบออนไลน์ของคลินิก";

    public const string CannotCancel = "ยกเลิกนัดนี้ไม่ได้ นัดอาจถูกยกเลิกไปแล้วหรือเริ่มไปแล้ว";

    public const string CancelReason = "ยกเลิกผ่านปุ่มใน LINE";

    public static string Reschedule(string portalUrl) => $"เลือกเวลานัดใหม่ได้ที่ {portalUrl}";

    public const string NoQueue = "วันนี้คุณไม่มีคิวที่รออยู่";

    public static string Queue(string number, string status, int ahead) =>
        ahead > 0 ? $"คิว {number} {status} รออีก {ahead} คิว" : $"คิว {number} {status}";

    public static string Invoice(string number, string total, string balance, string portalUrl) =>
        $"บิล {number} ยอด {total} บาท ค้างชำระ {balance} บาท ดูรายละเอียดได้ที่ {portalUrl}";

    public static string QueueStatus(Domain.Queueing.QueueTicketStatus status) => status switch
    {
        Domain.Queueing.QueueTicketStatus.Waiting => "รอเรียก",
        Domain.Queueing.QueueTicketStatus.Called => "กำลังเรียก เชิญที่ห้องตรวจ",
        Domain.Queueing.QueueTicketStatus.InRoom => "อยู่ในห้องตรวจ",
        _ => "เสร็จแล้ว",
    };
}
