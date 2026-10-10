using System.Globalization;
using System.Threading.RateLimiting;
using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// The rate limits stated in the contract: 600 requests per minute per token, and 60 per minute on
/// message-sending endpoints.
/// <para>
/// Partitioned by token subject rather than by IP, because a whole clinic sits behind one office
/// NAT — an IP-based limit would let one runaway script throttle every receptionist at the
/// counter. Unauthenticated traffic falls back to IP, which is all there is to key on.
/// </para>
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>Apply to messaging endpoints with <c>[EnableRateLimiting(RateLimitingExtensions.MessagingPolicy)]</c>.</summary>
    public const string MessagingPolicy = "messaging";

    /// <summary>
    /// The website assistant: every message costs a model call, and the endpoint is open to anyone,
    /// so it gets the tightest limit in the system (<c>Assistant:MessagesPerMinute</c>).
    /// </summary>
    public const string AssistantPolicy = "assistant";

    /// <summary>
    /// The staff AI assistant (<c>docs/contract-extensions.md § 34</c>): every call can cost a model
    /// call from an allowance the clinic shares, so one person is held to
    /// <c>StaffAi:CallsPerMinutePerUser</c> (10) and cannot spend the month in an afternoon.
    /// </summary>
    public const string StaffAiPolicy = "staff-ai";

    /// <summary>
    /// <c>POST /booking/guest/start</c> (<c>docs/contract-extensions.md § 27</c>): every call holds a
    /// vet's time for ten minutes and texts a code, for anyone, so it is capped per address
    /// (<c>RateLimits:GuestHoldsPerIpPerHour</c>, 30). Always keyed on the address, even for a caller
    /// with a token: the cap is on what one place on the internet can block.
    /// <para>
    /// The address is the connection's. Until the API reads forwarded headers from a trusted proxy
    /// (see <c>docs/security/README.md</c>), every request through the website arrives from the
    /// Next.js server, so this is one bucket for every visitor: a clinic-wide backstop, sized so a
    /// busy hour of real bookings does not reach it. The per-number code limit is per visitor already.
    /// </para>
    /// </summary>
    public const string GuestHoldPolicy = "guest-hold";

    /// <summary>
    /// <c>POST /booking/guest/verify</c>: code guesses per address (<c>RateLimits:GuestVerifiesPerIpPerHour</c>,
    /// 60), on top of the five guesses each code allows. Keyed the same way as <see cref="GuestHoldPolicy"/>.
    /// </summary>
    public const string GuestVerifyPolicy = "guest-verify";

    /// <summary>
    /// <c>POST /queue/displays/claim</c> (<c>docs/contract-extensions.md § 30</c>): setup-code guesses
    /// per address (<c>RateLimits:DisplayClaimsPerIpPerHour</c>, 30), keyed like the guest policies.
    /// A clinic sets up a handful of screens a year; thirty an hour is room for typos, not for guessing.
    /// </summary>
    public const string DisplayClaimPolicy = "display-claim";

    /// <summary>
    /// <c>GET /queue/wait-estimate</c> (<c>docs/contract-extensions.md § 37</c>): open to anyone, so it
    /// is capped per address (<c>RateLimits:WaitEstimatePerIpPerMinute</c>, 60) and keyed on the
    /// address even for a caller with a token. The answer is reused for 30 seconds, so a page that
    /// polls faster gains nothing; the website's server asks about once per branch per 30 seconds.
    /// </summary>
    public const string WaitEstimatePolicy = "wait-estimate";

    /// <summary>
    /// <c>POST /reviews/invitations/{token}</c> (<c>docs/contract-extensions.md § 36</c>): a review
    /// written through a one-time link, open to anyone holding one, so capped per address
    /// (<c>RateLimits:ReviewSubmitsPerIpPerHour</c>, 20), keyed like the guest policies. Each token
    /// writes one review at most; the cap is on how fast one place can try tokens.
    /// </summary>
    public const string ReviewSubmitPolicy = "review-submit";

    /// <summary>
    /// <c>GET /referrals/{code}</c> (<c>docs/contract-extensions.md § 39</c>): whether a referral code
    /// can be used, open to anyone, so capped per address (<c>RateLimits:ReferralChecksPerIpPerMinute</c>,
    /// 30) and keyed like the guest policies. The answer is only valid or not, and the codes are too
    /// many to guess; the cap is on how fast one place can try.
    /// </summary>
    public const string ReferralCheckPolicy = "referral-check";

    public static IServiceCollection AddClinicRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var perToken = configuration.GetValue("RateLimits:PerTokenPerMinute", 600);
        var messaging = configuration.GetValue("RateLimits:MessagingPerMinute", 60);
        var assistant = configuration.GetValue("Assistant:MessagesPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            // Read when the options are first built rather than at registration, so it sees every
            // configuration source the host ends up with.
            var perApiKey = configuration.GetValue("RateLimits:PerApiKeyPerMinute", perToken);
            var guestHolds = configuration.GetValue("RateLimits:GuestHoldsPerIpPerHour", 30);
            var guestVerifies = configuration.GetValue("RateLimits:GuestVerifiesPerIpPerHour", 60);
            var perDisplay = configuration.GetValue("RateLimits:PerDisplayPerMinute", 120);
            var displayClaims = configuration.GetValue("RateLimits:DisplayClaimsPerIpPerHour", 30);
            var staffAi = configuration.GetValue("StaffAi:CallsPerMinutePerUser", 10);
            var waitEstimates = configuration.GetValue("RateLimits:WaitEstimatePerIpPerMinute", 60);
            var reviewSubmits = configuration.GetValue("RateLimits:ReviewSubmitsPerIpPerHour", 20);
            var referralChecks = configuration.GetValue("RateLimits:ReferralChecksPerIpPerMinute", 30);

            // An API key gets its own window, apart from the person who made it: a sync job
            // running hot must not lock its creator out of the counter screen, nor the reverse.
            // A waiting-room screen gets its own too, keyed by the screen: it has no subject, and
            // falling back to the address would put it in one bucket with every visitor behind the
            // website's server. A screen reads the board once a minute and once per burst of events,
            // so 120 a minute is far above a busy morning (docs/contract-extensions.md § 30).
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => context.User.FindFirst(ClinicClaimTypes.ApiKey) is not null
                    ? Partition(context, perApiKey)
                    : context.User.FindFirst(ClinicClaimTypes.Display) is not null
                        ? Partition(context, perDisplay)
                        : Partition(context, perToken));

            options.AddPolicy(MessagingPolicy, context =>
                Partition(context, messaging, suffix: ":messaging"));

            options.AddPolicy(AssistantPolicy, context =>
                Partition(context, assistant, suffix: ":assistant"));

            options.AddPolicy(StaffAiPolicy, context =>
                Partition(context, staffAi, suffix: ":staff-ai"));

            options.AddPolicy(GuestHoldPolicy, context =>
                PerAddressPerHour(context, guestHolds, ":guest-hold"));

            options.AddPolicy(GuestVerifyPolicy, context =>
                PerAddressPerHour(context, guestVerifies, ":guest-verify"));

            options.AddPolicy(DisplayClaimPolicy, context =>
                PerAddressPerHour(context, displayClaims, ":display-claim"));

            options.AddPolicy(WaitEstimatePolicy, context =>
                PerAddressPerMinute(context, waitEstimates, ":wait-estimate"));

            options.AddPolicy(ReviewSubmitPolicy, context =>
                PerAddressPerHour(context, reviewSubmits, ":review-submit"));

            options.AddPolicy(ReferralCheckPolicy, context =>
                PerAddressPerMinute(context, referralChecks, ":referral-check"));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                await ApiErrorWriter.WriteAsync(
                    context.HttpContext,
                    HttpStatus.TooManyRequests,
                    ErrorCodes.RateLimited,
                    "มีคำขอมากเกินไป กรุณาลองใหม่อีกครั้ง",
                    messageEn: "Rate limit exceeded.");

                _ = cancellationToken;
            };
        });

        return services;
    }

    private static RateLimitPartition<string> PerAddressPerHour(HttpContext context, int permitPerHour, string suffix) =>
        RateLimitPartition.GetFixedWindowLimiter(
            (context.Connection.RemoteIpAddress?.ToString() ?? "unknown") + suffix,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            });

    private static RateLimitPartition<string> PerAddressPerMinute(HttpContext context, int permitPerMinute, string suffix) =>
        RateLimitPartition.GetFixedWindowLimiter(
            (context.Connection.RemoteIpAddress?.ToString() ?? "unknown") + suffix,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });

    private static RateLimitPartition<string> Partition(HttpContext context, int permitPerMinute, string suffix = "")
    {
        var subject = context.User.FindFirst(ClinicClaimTypes.ApiKey)?.Value
            ?? context.User.FindFirst(ClinicClaimTypes.Display)?.Value
            ?? context.User.FindFirst(ClinicClaimTypes.Subject)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            subject + suffix,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    }
}
