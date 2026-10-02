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

    public static IServiceCollection AddClinicRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var perToken = configuration.GetValue("RateLimits:PerTokenPerMinute", 600);
        var messaging = configuration.GetValue("RateLimits:MessagingPerMinute", 60);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => Partition(context, perToken));

            options.AddPolicy(MessagingPolicy, context =>
                Partition(context, messaging, suffix: ":messaging"));

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
        var subject = context.User.FindFirst(ClinicClaimTypes.Subject)?.Value
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
