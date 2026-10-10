using HappyPaws.Application.Assistant;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// Every staff AI call goes through here: is it switched on, is there allowance left, call the
/// model, and turn any failure into <c>503 ai_unavailable</c> with nothing from the provider in it.
/// <para>
/// <b>Order.</b> Switched on, then the caller's input is checked by the handler, then a use is
/// reserved, then the model is called. A request refused for its input or its rights never costs a
/// use; a cache hit never reaches here at all.
/// </para>
/// <para>
/// <b>Refunds.</b> When the model could not be reached or answered with an error, no answer was
/// produced and the provider normally bills nothing, so the use is given back. When the model did
/// answer, even with something this code then cannot use, the use stays counted: that answer was
/// billed.
/// </para>
/// <para>
/// <b>Logs</b> carry the feature, the rounds and the token counts. Never the prompt or the answer.
/// </para>
/// </summary>
public sealed class StaffAiGateway(
    IStaffLanguageModel model,
    StaffAiSettings settings,
    StaffAiQuota quota,
    ILogger<StaffAiGateway> logger)
{
    /// <summary>A provider is configured and the deployment allows at least one use a month.</summary>
    public bool IsEnabled => model.IsConfigured && settings.MonthlyCalls > 0;

    public StaffAiSettings Settings => settings;

    public void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new AiUnavailableException();
        }
    }

    public Task<StaffAiReservation> ReserveAsync(string feature, CancellationToken cancellationToken = default) =>
        quota.ReserveAsync(feature, cancellationToken);

    /// <summary>One question, one answer in words, counted as one use of <paramref name="feature"/>.</summary>
    public async Task<string> CompleteTextAsync(
        string feature,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        var reservation = await quota.ReserveAsync(feature, cancellationToken);
        var turn = await CallAsync(reservation, new ModelRequest(systemPrompt, [ModelMessage.User(userMessage)], []), answeredBefore: false, cancellationToken);

        logger.LogInformation(
            "Staff AI {Feature} answered in 1 round, tokens in {In} out {Out}",
            feature, turn.InputTokens, turn.OutputTokens);

        if (string.IsNullOrWhiteSpace(turn.Text))
        {
            logger.LogWarning("Staff AI {Feature} answered with no text", feature);
            throw new AiUnavailableException();
        }

        return turn.Text.Trim();
    }

    /// <summary>
    /// One model call under a reservation already made. A failure refunds the use only when
    /// <paramref name="answeredBefore"/> is false: once any round of this use has been answered, the
    /// use has cost something and stays counted.
    /// </summary>
    public async Task<ModelTurn> CallAsync(
        StaffAiReservation reservation,
        ModelRequest request,
        bool answeredBefore,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await model.CompleteAsync(request, cancellationToken);
        }
        catch (LanguageModelException failure)
        {
            logger.LogWarning(failure, "Staff AI {Feature} model call failed", reservation.Feature);

            if (!answeredBefore)
            {
                await quota.RefundAsync(reservation, CancellationToken.None);
            }

            throw new AiUnavailableException();
        }
    }
}
