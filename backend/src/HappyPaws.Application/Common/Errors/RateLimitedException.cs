namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// A <c>429 rate_limited</c> decided by a use case rather than by the request limiter — a limit
/// that counts something other than requests per token, such as codes sent to one phone number.
/// </summary>
public sealed class RateLimitedException(string messageTh)
    : AppException(ErrorCodes.RateLimited, messageTh, HttpStatus.TooManyRequests)
{
}
