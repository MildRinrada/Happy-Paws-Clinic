namespace HappyPaws.Api.Contracts.Messaging;

/// <summary>
/// <c>POST /staff/threads/{id}/messages</c> — "ตอบกลับ · ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด". The
/// words of the reply, 1 to 1,000 characters. The channel is not chosen by the caller.
/// </summary>
public sealed record ReplyToThreadRequest(string? Text = null);

/// <summary>
/// <c>POST /portal/thread/messages</c> — <b>contract extension</b> (<c>docs/contract-extensions.md § 24</c>).
/// The customer's words, 1 to 1,000 characters. Who is writing comes from the token.
/// </summary>
public sealed record WriteToClinicRequest(string? Text = null);
