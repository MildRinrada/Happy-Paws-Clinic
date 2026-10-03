using System.Text.Json;
using FluentValidation;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Assistant.Chat;

/// <summary>One message of the conversation so far, as the widget holds it.</summary>
public sealed record ChatMessageInput(string Role, string Text);

/// <summary>
/// <c>POST /assistant/chat</c> — a contract extension, <c>docs/contract-extensions.md § 16</c>. The
/// whole conversation so far is sent each time: nothing is stored on the server.
/// </summary>
public sealed record ChatCommand(IReadOnlyList<ChatMessageInput> Messages);

/// <summary>The assistant's reply, and the buttons the tools it used suggest.</summary>
public sealed record ChatReplyDto(string Reply, IReadOnlyList<AssistantAction> Actions);

public sealed class ChatValidator : AbstractValidator<ChatCommand>
{
    public const int MaxMessages = 20;
    public const int MaxLength = 1000;

    public ChatValidator()
    {
        RuleFor(command => command.Messages)
            .NotEmpty().WithMessage("ต้องมีข้อความอย่างน้อยหนึ่งข้อความ")
            .Must(messages => messages.Count <= MaxMessages).WithMessage($"บทสนทนายาวเกิน {MaxMessages} ข้อความ เริ่มบทสนทนาใหม่ได้")
            .Must(messages => messages.Count > 0 && messages[^1].Role == "user").WithMessage("ข้อความสุดท้ายต้องเป็นของผู้ใช้");

        RuleForEach(command => command.Messages).ChildRules(message =>
        {
            message.RuleFor(item => item.Role).Must(role => role is "user" or "assistant").WithMessage("role ต้องเป็น user หรือ assistant");
            message.RuleFor(item => item.Text)
                .NotEmpty().WithMessage("ข้อความว่างไม่ได้")
                .MaximumLength(MaxLength).WithMessage($"ข้อความยาวได้ไม่เกิน {MaxLength} ตัวอักษร");
        });
    }
}

/// <summary>
/// The assistant's loop: ask the model, run the tools it calls, hand the results back, until it
/// answers in words.
/// <para>
/// <b>Stateless and unlogged by design.</b> The widget sends the conversation each time and the
/// server keeps none of it, so there is no chat history to protect, retain or hand over under PDPA.
/// The log carries token counts and which tools ran, never what was said.
/// </para>
/// <para>
/// The buttons under a reply come from the tools that ran, not from the model's text: a model can
/// be talked into writing any link, and the widget only ever draws the ones this code produced.
/// </para>
/// </summary>
public sealed class ChatHandler(
    ILanguageModel model,
    IEnumerable<IAssistantTool> tools,
    AssistantBudget budget,
    AssistantSettings settings,
    GetClinicProfileHandler profile,
    RequestValidator validator,
    IClock clock,
    ILogger<ChatHandler> logger)
{
    private const string GiveUp = "ขอโทษค่ะ ตอนนี้ตอบคำถามนี้ไม่ได้ ติดต่อคลินิกทางโทรศัพท์หรือ LINE ได้เลย";

    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools = tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);

    public async Task<ChatReplyDto> HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (!model.IsConfigured)
        {
            throw new AssistantUnavailableException();
        }

        var clinic = await profile.HandleAsync(cancellationToken);
        var system = AssistantPrompt.Build(clinic.Name, clock.NowInClinicTime());
        var declarations = _tools.Values.Select(tool => new ModelTool(tool.Name, tool.Description, tool.ParametersSchema)).ToList();

        var messages = command.Messages
            .Select(message => message.Role == "user" ? ModelMessage.User(message.Text.Trim()) : ModelMessage.Assistant(message.Text.Trim()))
            .ToList();

        var actions = new List<AssistantAction>();
        var used = new List<string>();
        var (inputTokens, outputTokens) = (0, 0);

        for (var round = 0; round <= settings.MaxToolRounds; round++)
        {
            budget.Take();

            ModelTurn turn;
            try
            {
                turn = await model.CompleteAsync(new ModelRequest(system, messages, declarations), cancellationToken);
            }
            catch (LanguageModelException failure)
            {
                logger.LogWarning(failure, "Assistant model call failed after tools [{Tools}]", string.Join(",", used));
                throw new AssistantUnavailableException();
            }

            inputTokens += turn.InputTokens;
            outputTokens += turn.OutputTokens;

            if (turn.ToolCalls.Count == 0)
            {
                logger.LogInformation(
                    "Assistant replied in {Rounds} round(s), tools [{Tools}], tokens in {In} out {Out}",
                    round + 1, string.Join(",", used), inputTokens, outputTokens);

                var reply = string.IsNullOrWhiteSpace(turn.Text) ? GiveUp : turn.Text.Trim();
                return new ChatReplyDto(reply, Distinct(actions));
            }

            messages.Add(ModelMessage.Calls(turn.ToolCalls));

            foreach (var call in turn.ToolCalls)
            {
                used.Add(call.Name);
                var result = await RunAsync(call, cancellationToken);

                actions.AddRange(result.Actions ?? []);
                messages.Add(ModelMessage.Result(call, JsonSerializer.Serialize(result.Data)));
            }
        }

        logger.LogWarning("Assistant gave up after {Rounds} tool rounds, tools [{Tools}]", settings.MaxToolRounds, string.Join(",", used));

        return new ChatReplyDto(GiveUp, [new AssistantAction("ดูช่องทางติดต่อ", "/contact")]);
    }

    private async Task<ToolResult> RunAsync(ModelToolCall call, CancellationToken cancellationToken)
    {
        if (!_tools.TryGetValue(call.Name, out var tool))
        {
            return new ToolResult(new { error = $"no tool named {call.Name}" });
        }

        JsonElement arguments;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            arguments = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new ToolResult(new { error = "arguments were not valid JSON" });
        }

        return await tool.InvokeAsync(arguments, cancellationToken);
    }

    private static IReadOnlyList<AssistantAction> Distinct(List<AssistantAction> actions) =>
        [.. actions.DistinctBy(action => action.Href).Take(3)];
}

/// <summary><c>GET /assistant</c> — whether the website should draw the chat button at all.</summary>
public sealed record AssistantStatusDto(bool Enabled);

public sealed class GetAssistantStatusHandler(ILanguageModel model)
{
    public AssistantStatusDto Handle() => new(model.IsConfigured);
}
