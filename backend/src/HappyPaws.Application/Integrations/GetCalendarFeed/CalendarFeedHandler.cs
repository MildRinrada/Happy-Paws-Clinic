using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Identity.Capabilities;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.GetCalendarFeed;

/// <summary>Which appointments a feed holds.</summary>
public enum CalendarView
{
    /// <summary>The caller's own appointments, as the assigned vet. Any staff member.</summary>
    Mine,

    /// <summary>Every appointment in the caller's branches. Needs <c>appointment:read</c>.</summary>
    Branch,
}

/// <summary>An iCalendar file, ready to send.</summary>
public sealed record CalendarFile(string Content, string FileName);

/// <summary>
/// <c>GET /staff/integrations/calendar/ics</c>, in its three forms.
/// <list type="bullet">
///   <item><description><c>?token=hpcal_…</c>, no login: the feed a calendar app subscribes to.</description></item>
///   <item><description>Signed in, <c>Accept: text/calendar</c> (the default): the same feed, for a one-off download.</description></item>
///   <item><description>Signed in, <c>Accept: application/json</c>: the caller's subscription link, made the first time it is asked for.</description></item>
/// </list>
/// <para>
/// <b>Who.</b> Any active staff member, for their own appointments; <c>?view=branch</c> also needs
/// <c>appointment:read</c> and stays inside the branches the person works at. The contract names
/// no scope for this endpoint; why none is required for the own view is in
/// <c>docs/assumptions.md § 2</c>. API keys are refused: a feed is a person's.
/// </para>
/// <para>
/// <b>What.</b> From 30 days back to 180 days ahead, at most <see cref="MaxEntries"/>
/// appointments: service, pet name, branch and time. Nothing clinical and no owner contact data,
/// because a feed URL ends up in calendar apps, and calendar apps sync to phones and share.
/// </para>
/// </summary>
public sealed class CalendarFeedHandler(
    ICalendarFeedRepository feeds,
    IStaffRepository staff,
    IIdentityRepository identity,
    EffectiveScopeResolver scopeResolver,
    ISecretProtector protector,
    ICurrentUser currentUser,
    BranchGuard branchGuard,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxEntries = 2_000;

    public static readonly TimeSpan Lookback = TimeSpan.FromDays(30);

    public static readonly TimeSpan Lookahead = TimeSpan.FromDays(180);

    public const string FeedPath = "/staff/integrations/calendar/ics";

    /// <summary>The signed-in staff member's subscription link, made on first request.</summary>
    public async Task<CalendarLinkDto> GetLinkAsync(CancellationToken cancellationToken = default)
    {
        var staffId = RequirePerson();

        var feed = await feeds.FindByStaffAsync(staffId, cancellationToken);

        if (feed is null)
        {
            var token = CalendarFeed.NewToken();
            feed = CalendarFeed.Create(staffId, token, protector.Protect(token, SecretPurposes.CalendarFeed));
            feeds.Add(feed);

            audit.Record(new AuditEvent(AuditActions.CalendarFeedCreated, AuditResources.Integration, feed.Id));

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var plaintext = protector.Unprotect(feed.TokenCiphertext, SecretPurposes.CalendarFeed);

        return new CalendarLinkDto(
            $"{FeedPath}?token={plaintext}",
            plaintext,
            currentUser.Scopes.Allows(Scopes.Appointment.Read),
            feed.CreatedAt,
            feed.LastUsedAt);
    }

    /// <summary>The signed-in staff member's feed.</summary>
    public async Task<CalendarFile> RenderForCallerAsync(CalendarView view, CancellationToken cancellationToken = default)
    {
        var staffId = RequirePerson();

        if (view == CalendarView.Branch)
        {
            currentUser.RequireScopes(Scopes.Appointment.Read);
        }

        return await RenderAsync(staffId, view, branchGuard.VisibleBranchIds, cancellationToken);
    }

    /// <summary>The feed behind a subscription link, or <c>401</c> for a token that is not one.</summary>
    public async Task<CalendarFile> RenderForTokenAsync(string token, CalendarView view, CancellationToken cancellationToken = default)
    {
        var feed = token.StartsWith(CalendarFeed.TokenPrefix, StringComparison.Ordinal) && token.Length < 100
            ? await feeds.FindByTokenHashAsync(ApiKey.Hash(token), cancellationToken)
            : null;

        if (feed is null)
        {
            throw Unauthenticated();
        }

        var membership = await staff.FindTrackedByIdAsync(feed.StaffId, cancellationToken);
        var account = membership is { IsActive: true }
            ? await identity.FindByIdAsync(membership.UserId, cancellationToken)
            : null;

        if (membership is null || account is not { CanAuthenticate: true })
        {
            throw Unauthenticated();
        }

        var held = await scopeResolver.ResolveAsync(membership, customerId: null, cancellationToken);

        if (view == CalendarView.Branch && !held.Allows(Scopes.Appointment.Read))
        {
            throw new InsufficientScopeException([Scopes.Appointment.Read]);
        }

        var branches = held.IsUnrestricted || membership.BranchIds.Count == 0 ? null : membership.BranchIds;

        var file = await RenderAsync(membership.Id, view, branches, cancellationToken);

        var now = clock.UtcNow;
        if (feed.LastUsedAt is not { } last || now - last >= TimeSpan.FromMinutes(5))
        {
            await feeds.TouchAsync(feed.Id, now, cancellationToken);
        }

        return file;
    }

    private async Task<CalendarFile> RenderAsync(
        string staffId,
        CalendarView view,
        IReadOnlyCollection<string>? branches,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var entries = await feeds.AppointmentsAsync(
            view == CalendarView.Mine ? staffId : null,
            branches,
            now - Lookback,
            now + Lookahead,
            MaxEntries,
            cancellationToken);

        var clinic = await settings.ProfileAsync(cancellationToken);
        var name = string.IsNullOrWhiteSpace(clinic.Name) ? "Happy Paws Clinic" : clinic.Name;
        var title = view == CalendarView.Mine ? $"{name} (นัดของฉัน)" : $"{name} (นัดทั้งสาขา)";

        return new CalendarFile(
            IcsCalendar.Write(title, "happypaws", entries, now),
            view == CalendarView.Mine ? "my-appointments.ics" : "branch-appointments.ics");
    }

    /// <summary>A signed-in staff member, not an API key.</summary>
    private string RequirePerson()
    {
        if (currentUser.ApiKeyId is not null)
        {
            throw new ForbiddenException("an API key has no calendar of its own");
        }

        return currentUser.RequireStaffId();
    }

    private static UnauthenticatedException Unauthenticated() => new();
}

/// <summary>A refused calendar token: <c>401 unauthenticated</c>, the same as an expired login.</summary>
public sealed class UnauthenticatedException()
    : AppException(ErrorCodes.Unauthenticated, "ลิงก์ปฏิทินไม่ถูกต้องหรือถูกยกเลิกแล้ว", HttpStatus.Unauthorized)
{
}
