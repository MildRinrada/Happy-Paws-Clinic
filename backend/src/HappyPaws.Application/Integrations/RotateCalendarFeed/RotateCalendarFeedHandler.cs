using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.RotateCalendarFeed;

/// <summary>
/// <c>POST /staff/integrations/calendar/token</c> — a new calendar link for the signed-in staff
/// member; the old link stops working at once. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>.
/// <para>
/// The same rule as the feed: one's own link, any staff member, no scope, never an API key. There
/// is no way to replace someone else's link; deactivating them already stops theirs. Calendars
/// subscribed with the old link stop updating and must be given the new one. Answers the link in
/// the same shape as <c>GET …/calendar/ics</c> with <c>Accept: application/json</c>. Audited
/// (never the token). With no link yet, this makes the first one.
/// </para>
/// </summary>
public sealed class RotateCalendarFeedHandler(
    ICalendarFeedRepository feeds,
    ISecretProtector protector,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CalendarLinkDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.ApiKeyId is not null)
        {
            throw new ForbiddenException("an API key has no calendar of its own");
        }

        var staffId = currentUser.RequireStaffId();
        var token = CalendarFeed.NewToken();
        var ciphertext = protector.Protect(token, SecretPurposes.CalendarFeed);
        var createdAt = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async cancel =>
        {
            var now = clock.UtcNow;
            var existing = await feeds.FindByStaffAsync(staffId, cancel);

            if (existing is null)
            {
                var feed = CalendarFeed.Create(staffId, token, ciphertext);
                feeds.Add(feed);
                createdAt = now;

                audit.Record(new AuditEvent(AuditActions.CalendarFeedCreated, AuditResources.Integration, feed.Id));
                return;
            }

            await feeds.ReplaceTokenAsync(existing.Id, ApiKey.Hash(token), ciphertext, now, cancel);
            createdAt = existing.CreatedAt;

            audit.Record(new AuditEvent(AuditActions.CalendarFeedRotated, AuditResources.Integration, existing.Id));
        }, cancellationToken);

        return new CalendarLinkDto(
            $"{CalendarFeedHandler.FeedPath}?token={token}",
            token,
            currentUser.Scopes.Allows(Scopes.Appointment.Read),
            createdAt,
            LastUsedAt: null);
    }
}
