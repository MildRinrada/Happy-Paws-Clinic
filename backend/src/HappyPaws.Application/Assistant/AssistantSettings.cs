using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Assistant;

/// <summary>
/// How much the assistant may do, from <c>Assistant:*</c> in configuration.
/// </summary>
/// <param name="DailyConversationTurns">
/// Model calls allowed per clinic day across every visitor. The bill's ceiling: a clinic sold a
/// package of so many conversations a month should not be the one paying for a script that found
/// the chat box. Zero means no ceiling.
/// </param>
/// <param name="MaxToolRounds">How many times one reply may go back to the tools before it gives up.</param>
public sealed record AssistantSettings(int DailyConversationTurns = 1000, int MaxToolRounds = 4);

/// <summary>
/// Counts model calls against the daily ceiling, in memory.
/// <para>
/// One clinic is one deployment and one API process, so a process-local counter is the whole
/// clinic's count. It starts again at midnight in clinic time, and after a restart — which errs on
/// the side of the visitor, not the bill, by at most one day's allowance.
/// </para>
/// </summary>
public sealed class AssistantBudget(AssistantSettings settings, IClock clock)
{
    private readonly object _gate = new();
    private DateOnly _day;
    private int _used;

    public void Take()
    {
        if (settings.DailyConversationTurns <= 0) return;

        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);

        lock (_gate)
        {
            if (today != _day)
            {
                _day = today;
                _used = 0;
            }

            if (_used >= settings.DailyConversationTurns)
            {
                throw new RateLimitedException("ผู้ช่วยตอบครบจำนวนของวันนี้แล้ว ติดต่อคลินิกทางโทรศัพท์หรือ LINE ได้เลย");
            }

            _used++;
        }
    }
}

/// <summary><c>503 assistant_unavailable</c> — switched off, or the model could not answer.</summary>
public sealed class AssistantUnavailableException : AppException
{
    public AssistantUnavailableException()
        : base("assistant_unavailable", "ผู้ช่วยตอบไม่ได้ในตอนนี้ ติดต่อคลินิกทางโทรศัพท์หรือ LINE ได้เลย", HttpStatus.ServiceUnavailable)
    {
        MessageEn = "The assistant is not available.";
    }
}
