namespace HappyPaws.Api.Contracts.Assistant;

/// <summary>
/// <c>POST /assistant/chat</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 16</c>. The whole conversation so far, oldest first, the last
/// message the visitor's. At most 20 messages of 1,000 characters each.
/// </summary>
public sealed record ChatRequest(IReadOnlyList<ChatMessageRequest> Messages);

/// <summary><c>role</c> is <c>user</c> or <c>assistant</c>.</summary>
public sealed record ChatMessageRequest(string Role, string Text);
