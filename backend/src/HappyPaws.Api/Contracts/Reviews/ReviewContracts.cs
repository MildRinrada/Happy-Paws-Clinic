namespace HappyPaws.Api.Contracts.Reviews;

/// <summary>
/// <c>POST /portal/reviews</c> (<c>docs/contract-extensions.md § 36</c>):
/// <code>{ "visit_id": "inv_…", "rating": 5, "text": "หมอใจดีมาก", "display_name": "first_name" }</code>
/// </summary>
public sealed record SubmitPortalReviewRequest(
    string? VisitId = null,
    int? Rating = null,
    string? Text = null,
    string? DisplayName = null);

/// <summary>
/// <c>POST /reviews/invitations/{token}</c>:
/// <code>{ "rating": 4, "text": "…", "display_name": "anonymous" }</code>
/// </summary>
public sealed record SubmitInvitationReviewRequest(int? Rating = null, string? Text = null, string? DisplayName = null);

/// <summary>
/// <c>POST /staff/reviews/{id}/reject</c>: a reason from the list, nothing free-form:
/// <code>{ "reason": "personal_data" }</code>
/// </summary>
public sealed record RejectReviewRequest(string? Reason = null);

/// <summary><c>POST /staff/reviews/{id}/reply</c>: <code>{ "text": "ขอบคุณค่ะ" }</code></summary>
public sealed record ReplyToReviewRequest(string? Text = null);
