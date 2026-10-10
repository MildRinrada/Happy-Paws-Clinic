namespace HappyPaws.Api.Contracts.Queueing;

/// <summary>
/// <c>POST /staff/queue/displays</c> — a waiting-room screen for one branch. <c>expires_at</c> is
/// optional; without it the screen works until revoked. A contract extension, see
/// <c>docs/contract-extensions.md § 30</c>.
/// </summary>
public sealed record CreateQueueDisplayRequest(string? Name, string? BranchId, DateTimeOffset? ExpiresAt = null);

/// <summary><c>POST /queue/displays/claim</c> — the setup code shown in the console.</summary>
public sealed record ClaimQueueDisplayRequest(string? Code);
