using HappyPaws.Application.Analytics.GetAppointmentStats;
using HappyPaws.Application.Analytics.GetQueueStats;
using HappyPaws.Application.Analytics.GetRevenueAnalytics;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Inventory.ListAlerts;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Clinics;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.StaffAi.DailyBrief;

/// <summary>What a stored brief's row says about it.</summary>
public static class DailyBriefStatus
{
    /// <summary>A worker has claimed it and is writing it now.</summary>
    public const string Generating = "generating";

    public const string Ready = "ready";

    /// <summary>The numbers may be there; the words are not. <c>failure_reason</c> says why.</summary>
    public const string Failed = "failed";
}

/// <summary>Why a brief has no words.</summary>
public static class DailyBriefFailure
{
    /// <summary>The month's AI allowance was spent. Not retried that day.</summary>
    public const string QuotaExceeded = "quota_exceeded";

    /// <summary>The model failed. Retried on the next run, up to <see cref="GenerateDailyBriefsHandler.MaxAttempts"/> tries.</summary>
    public const string ModelUnavailable = "model_unavailable";

    /// <summary>A report behind the numbers failed. Retried like a model failure.</summary>
    public const string NumbersFailed = "numbers_failed";
}

/// <summary>One branch's brief for one clinic day, as stored.</summary>
public sealed record StoredDailyBrief(
    string BranchId,
    DateOnly Date,
    string Status,
    string? FailureReason,
    DailyBriefNumbersDto? Numbers,
    string? Text,
    DateTimeOffset? GeneratedAt);

/// <summary>The briefs, one row per branch per clinic day.</summary>
public interface IDailyBriefStore
{
    /// <summary>
    /// Claims the brief for a branch and day, in one statement: creates it as generating, or takes
    /// back one that failed and may be tried again, or one left generating since before
    /// <paramref name="staleBefore"/> by a worker that stopped. False when another worker has it or
    /// it is done. This is what makes "once per branch per day" hold across instances.
    /// </summary>
    Task<bool> TryClaimAsync(
        string branchId,
        DateOnly date,
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task FinishAsync(
        string branchId,
        DateOnly date,
        string status,
        string? failureReason,
        DailyBriefNumbersDto? numbers,
        string? text,
        DateTimeOffset at,
        CancellationToken cancellationToken = default);

    Task<StoredDailyBrief?> FindAsync(string branchId, DateOnly date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes the day's morning brief for every branch, once per branch per clinic day, from the hour
/// set by <c>StaffAi:DailyBriefHour</c> (07:00 by default). Run by the scheduler every few minutes.
/// <para>
/// <b>Numbers first, by code.</b> Today's appointments and queue, the stock alerts and yesterday's
/// takings come from the same use cases as the dashboard and the reports, run with no caller (the
/// scheduler acts for the clinic, so every branch is in reach). The model is then asked for a few
/// sentences around them. If it fails or the allowance is spent, the numbers are stored anyway and
/// the dashboard shows them without words.
/// </para>
/// <para>
/// <b>Missed hours are caught up.</b> An instance that was down at 07:00 writes the brief at its
/// next run that day. A brief is never written for a day already past.
/// </para>
/// </summary>
public sealed class GenerateDailyBriefsHandler(
    IBranchRepository branches,
    IDailyBriefStore store,
    StaffAiGateway gateway,
    GetAppointmentStatsHandler appointments,
    GetQueueStatsHandler queue,
    GetRevenueAnalyticsHandler revenue,
    ListAlertsHandler alerts,
    GetClinicProfileHandler profile,
    IClock clock,
    ILogger<GenerateDailyBriefsHandler> logger)
{
    public const int MaxAttempts = 3;

    /// <summary>A brief still generating after this long belonged to a worker that stopped.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>Writes the briefs due at <paramref name="nowUtc"/>. Returns how many it wrote, words or not.</summary>
    public async Task<int> RunAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        if (!gateway.IsEnabled)
        {
            return 0;
        }

        var local = TimeZoneInfo.ConvertTime(nowUtc, clock.ClinicTimeZone);

        if (local.Hour < Math.Clamp(gateway.Settings.DailyBriefHour, 0, 23))
        {
            return 0;
        }

        var date = DateOnly.FromDateTime(local.DateTime);
        var written = 0;

        foreach (var branch in await branches.ListActiveAsync(cancellationToken))
        {
            if (!await store.TryClaimAsync(branch.Id, date, nowUtc, nowUtc - StaleAfter, MaxAttempts, cancellationToken))
            {
                continue;
            }

            await WriteAsync(branch, date, cancellationToken);
            written++;
        }

        return written;
    }

    private async Task WriteAsync(Branch branch, DateOnly date, CancellationToken cancellationToken)
    {
        DailyBriefNumbersDto numbers;

        try
        {
            numbers = DailyBriefAssembler.Numbers(
                await appointments.HandleAsync(new GetAppointmentStatsQuery(date, date, branch.Id), cancellationToken),
                await queue.HandleAsync(new GetQueueStatsQuery(date, date, branch.Id), cancellationToken),
                await alerts.HandleAsync(new ListAlertsQuery(branch.Id), cancellationToken),
                await revenue.HandleAsync(new GetRevenueAnalyticsQuery(date.AddDays(-1), date.AddDays(-1), branch.Id), cancellationToken));
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogError(failure, "Daily brief numbers for branch {BranchId} on {Date} failed", branch.Id, date);
            await store.FinishAsync(branch.Id, date, DailyBriefStatus.Failed, DailyBriefFailure.NumbersFailed, null, null, clock.UtcNow, cancellationToken);
            return;
        }

        try
        {
            var clinic = await profile.HandleAsync(cancellationToken);
            var text = await gateway.CompleteTextAsync(
                StaffAiFeatures.DailyBrief,
                DailyBriefPrompt.System(clinic.Name),
                StaffAiPromptRules.Fence(DailyBriefAssembler.PromptData(branch.Name, date, numbers)),
                cancellationToken);

            await store.FinishAsync(branch.Id, date, DailyBriefStatus.Ready, null, numbers, text, clock.UtcNow, cancellationToken);
        }
        catch (AiQuotaExceededException)
        {
            logger.LogWarning("Daily brief for branch {BranchId} on {Date} has no words: the month's AI allowance is spent", branch.Id, date);
            await store.FinishAsync(branch.Id, date, DailyBriefStatus.Failed, DailyBriefFailure.QuotaExceeded, numbers, null, clock.UtcNow, cancellationToken);
        }
        catch (AiUnavailableException)
        {
            await store.FinishAsync(branch.Id, date, DailyBriefStatus.Failed, DailyBriefFailure.ModelUnavailable, numbers, null, clock.UtcNow, cancellationToken);
        }
    }
}

/// <summary>
/// <c>GET /staff/ai/daily-brief?branch_id=</c>. Always <c>200</c> with a <c>status</c>:
/// <list type="bullet">
/// <item><c>ready</c>: <c>text</c> and <c>numbers</c> are there.</item>
/// <item><c>failed</c>: <c>numbers</c> may be there, <c>text</c> is not; <c>reason</c> is
/// <c>quota_exceeded</c>, <c>model_unavailable</c> or <c>numbers_failed</c>.</item>
/// <item><c>pending</c>: not written yet today (before <c>generates_at</c>, or being written now).</item>
/// <item><c>disabled</c>: the AI is off on this deployment and nothing was written today. The page hides the card.</item>
/// </list>
/// <c>numbers.yesterday_revenue</c> is present only for a caller who may read money
/// (<c>analytics:read</c> or <c>invoice:write</c>), the dashboard's own rule.
/// </summary>
public sealed record DailyBriefDto(
    string Status,
    string BranchId,
    DateOnly Date,
    string GeneratesAt,
    string? Text = null,
    DailyBriefNumbersDto? Numbers = null,
    DateTimeOffset? GeneratedAt = null,
    string? Reason = null);

/// <summary>
/// Reads today's stored brief. Never calls the model and never counts a use: the brief is
/// written once by the scheduler and read by everyone.
/// </summary>
public sealed class GetDailyBriefHandler(
    IDailyBriefStore store,
    IBranchRepository branches,
    BranchGuard branchGuard,
    StaffAiGateway gateway,
    ICurrentUser currentUser,
    ClinicDays days)
{
    public async Task<DailyBriefDto> HandleAsync(string? branchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(branchId) || branchId.Length > 40)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["branch_id"] = ["ต้องระบุ branch_id ของสาขา"] });
        }

        branchId = branchId.Trim();
        branchGuard.EnsureCanAccess(branchId);

        if (!(await branches.ListActiveAsync(cancellationToken)).Any(branch => branch.Id == branchId))
        {
            throw new NotFoundException("branch", branchId);
        }

        var today = days.Today;
        var generatesAt = $"{Math.Clamp(gateway.Settings.DailyBriefHour, 0, 23):00}:00";
        var stored = await store.FindAsync(branchId, today, cancellationToken);

        if (stored is null || stored.Status == DailyBriefStatus.Generating)
        {
            return new DailyBriefDto(stored is null && !gateway.IsEnabled ? "disabled" : "pending", branchId, today, generatesAt);
        }

        var mayReadMoney = currentUser.Scopes.Allows(Scopes.Analytics.Read) || currentUser.Scopes.Allows(Scopes.Invoice.Write);
        var numbers = stored.Numbers is { } found && !mayReadMoney ? found with { YesterdayRevenue = null } : stored.Numbers;

        return new DailyBriefDto(
            stored.Status,
            branchId,
            today,
            generatesAt,
            stored.Text,
            numbers,
            stored.GeneratedAt,
            stored.FailureReason);
    }
}
