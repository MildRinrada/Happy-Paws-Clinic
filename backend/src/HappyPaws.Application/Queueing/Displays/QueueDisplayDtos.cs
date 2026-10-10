using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>
/// A waiting-room screen in <c>GET /staff/queue/displays</c>. Never the token, the setup code or
/// either hash. <c>status</c> is <c>pending_setup</c>, <c>setup_expired</c>, <c>active</c>,
/// <c>expired</c> or <c>revoked</c>.
/// </summary>
public sealed record QueueDisplayDto(
    string Id,
    string Name,
    string BranchId,
    string Status,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SetupExpiresAt,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? RevokedAt)
{
    public static QueueDisplayDto From(QueueDisplay display, DateTimeOffset now) =>
        new(
            display.Id,
            display.Name,
            display.BranchId,
            display.StateAt(now),
            display.CreatedByStaffId,
            display.CreatedAt,
            display.TokenHash is null ? display.SetupExpiresAt : null,
            display.ClaimedAt,
            display.ExpiresAt,
            display.LastSeenAt,
            display.RevokedAt);
}

/// <summary>
/// <c>201</c> of <c>POST /staff/queue/displays</c>: the display, and its setup code this once.
/// The code is typed on the TV, or followed as a link, within <c>setup_expires_at</c>.
/// </summary>
public sealed record CreatedQueueDisplayDto(
    string Id,
    string Name,
    string BranchId,
    string Status,
    string SetupCode,
    DateTimeOffset SetupExpiresAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// <c>POST /queue/displays/claim</c>: the token, this once, for the TV's server to keep. The only
/// answer in the API that carries it.
/// </summary>
public sealed record ClaimedQueueDisplayDto(
    string DisplayId,
    string Name,
    string BranchId,
    string Token,
    DateTimeOffset? ExpiresAt);

/// <summary>
/// <c>GET /queue/display-board</c>: what a public screen may show, and nothing more.
/// <para>
/// Ticket numbers, where each called ticket should go, which vet is in which room, and how many
/// are waiting. No ticket, pet, customer, service or appointment id, no pet or owner name, no
/// priority, no reason. <c>skipped</c> is shown as <c>waiting</c>: the room does not need to know
/// who missed their call. <c>docs/contract-extensions.md § 30</c>.
/// </para>
/// </summary>
public sealed record DisplayBoardDto(
    string BranchId,
    string BranchName,
    DateOnly ServiceDate,
    int WaitingCount,
    IReadOnlyList<string> ExamRooms,
    IReadOnlyList<DisplayBoardTicketDto> Tickets);

/// <summary>
/// One ticket on the screen, in call order. <c>room_id</c>, <c>vet_name</c> and <c>called_at</c>
/// are sent only for <c>called</c> and <c>in_room</c>, the tickets the screen says something about.
/// </summary>
public sealed record DisplayBoardTicketDto(
    string Number,
    string Status,
    string? RoomId = null,
    string? VetName = null,
    DateTimeOffset? CalledAt = null);
