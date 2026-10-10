using HappyPaws.Api.Contracts.Assistant;

namespace HappyPaws.Api.Contracts.StaffAi;

/// <summary>
/// <c>POST /staff/ai/consultations/{id}/soap-draft</c>: <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 34</c>. The vet's short notes, 1 to 4,000 characters.
/// </summary>
public sealed record SoapDraftRequest(string? Notes = null);

/// <summary>
/// <c>POST /staff/ai/drafts/owner-message</c>: <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 34</c>. <c>kind</c> is <c>aftercare</c> with
/// <c>consultation_id</c>, or <c>thread_reply</c> with <c>thread_id</c>.
/// </summary>
public sealed record OwnerMessageDraftRequest(string? Kind = null, string? ConsultationId = null, string? ThreadId = null);

/// <summary>
/// <c>POST /staff/ai/ask</c>: <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 34</c>. The same shape as <c>POST /assistant/chat</c>.
/// </summary>
public sealed record AskRequest(IReadOnlyList<ChatMessageRequest>? Messages = null);
