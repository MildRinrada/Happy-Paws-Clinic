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

            // An API key gets its own window, apart from the person who made it: a sync job
            // running hot must not lock its creator out of the counter screen, nor the reverse.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => context.User.FindFirst(ClinicClaimTypes.ApiKey) is not null
                    ? Partition(context, perApiKey)
                    : Partition(context, perToken));

            options.AddPolicy(MessagingPolicy, context =>
                Partition(context, messaging, suffix: ":messaging"));

            options.AddPolicy(AssistantPolicy, context =>
                Partition(context, assistant, suffix: ":assistant"));

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

    private static RateLimitPartition<string> Partition(HttpContext context, int permitPerMinute, string suffix = "")
    {
        var subject = context.User.FindFirst(ClinicClaimTypes.ApiKey)?.Value
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
