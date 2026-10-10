using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// <c>503 ai_unavailable</c>: the staff AI assistant is switched off on this deployment, or the model
/// failed or answered with something unusable. The provider's own words never reach the caller;
/// they go to the log as a status code and the start of a body.
/// </summary>
public sealed class AiUnavailableException : AppException
{
    public AiUnavailableException()
        : base(ErrorCodes.AiUnavailable, "ผู้ช่วย AI ใช้งานไม่ได้ในตอนนี้ ลองใหม่อีกครั้งภายหลัง", HttpStatus.ServiceUnavailable)
    {
        MessageEn = "The AI assistant is not available.";
    }
}

/// <summary>
/// <c>429 ai_quota_exceeded</c>: this month's allowance for the deployment is used up. The limit is
/// set by whoever runs the deployment, not by the clinic, so the message says to wait for next month.
/// </summary>
public sealed class AiQuotaExceededException : AppException
{
    public AiQuotaExceededException(string month, int limit)
        : base(
            ErrorCodes.AiQuotaExceeded,
            $"ใช้ผู้ช่วย AI ครบ {limit:N0} ครั้งของเดือนนี้แล้ว ใช้ได้อีกครั้งในเดือนถัดไป",
            HttpStatus.TooManyRequests)
    {
        MessageEn = "This month's AI allowance is used up.";
        Details = new Dictionary<string, object> { ["month"] = month, ["limit"] = limit, ["used"] = limit };
    }
}
