namespace HappyPaws.Api.Contracts.Messaging;

/// <summary>
/// <c>POST /staff/threads/{id}/messages</c> — "ตอบกลับ · ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด". The
/// words of the reply, 1 to 1,000 characters. The channel is not chosen by the caller.
/// </summary>
public sealed record ReplyToThreadRequest(string? Text = null);
