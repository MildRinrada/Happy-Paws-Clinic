namespace HappyPaws.Application.Integrations.Dtos;

/// <summary>
/// The JSON form of <c>GET /staff/integrations/calendar/ics</c>: the signed-in staff member's
/// subscription link. <see cref="Path"/> is relative to the API base URL (the one ending in
/// <c>/v1</c>); add <c>&amp;view=branch</c> for the whole branch when <see cref="CanViewBranch"/>.
/// The token is a credential: anyone holding the link sees the appointments in the feed.
/// </summary>
public sealed record CalendarLinkDto(
    string Path,
    string Token,
    bool CanViewBranch,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);
