using System.Text.Json;
using FluentValidation;
using HappyPaws.Application.Assistant;
using HappyPaws.Application.Assistant.Chat;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Clinics.ListBranches;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.StaffAi.Privacy;
using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Domain.Auditing;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.StaffAi.Ask;

/// <summary>
/// <c>POST /staff/ai/ask</c>: the same shape as <c>POST /assistant/chat</c>, the whole conversation
/// so far each time, the last message the caller's. Nothing is stored on the server.
/// </summary>
public sealed record AskCommand(IReadOnlyList<ChatMessageInput> Messages);

public sealed class AskValidator : AbstractValidator<AskCommand>
{
    public AskValidator()
    {
        RuleFor(command => command.Messages)
            .NotEmpty().WithMessage("ต้องมีข้อความอย่างน้อยหนึ่งข้อความ")
            .Must(messages => messages.Count <= ChatValidator.MaxMessages).WithMessage($"บทสนทนายาวเกิน {ChatValidator.MaxMessages} ข้อความ เริ่มบทสนทนาใหม่ได้")
            .Must(messages => messages.Count > 0 && messages[^1].Role == "user").WithMessage("ข้อความสุดท้ายต้องเป็นของผู้ใช้");

        RuleForEach(command => command.Messages).ChildRules(message =>
        {
            message.RuleFor(item => item.Role).Must(role => role is "user" or "assistant").WithMessage("role ต้องเป็น user หรือ assistant");
            message.RuleFor(item => item.Text)
                .NotEmpty().WithMessage("ข้อความว่างไม่ได้")
                .MaximumLength(ChatValidator.MaxLength).WithMessage($"ข้อความยาวได้ไม่เกิน {ChatValidator.MaxLength} ตัวอักษร");
        });
    }
}

/// <summary>
/// Answers questions about the clinic's numbers from its reports, as the person asking.
/// <para>
/// <b>Never more than the caller can see.</b> The model is offered only the tools whose scope the
/// caller holds, a call to any other tool is answered with an error and never runs, and each tool
/// runs its use case in this request, so the caller's branches apply exactly as on the report's own
/// screen.
/// </para>
/// <para>
/// <b>Names.</b> Every staff name is registered with the pseudonymizer before anything is sent, so
/// a name in a report, in the question or in an earlier answer reaches the model as
/// <c>[STAFF_n]</c>, and is put back in the reply.
/// </para>
/// <para>
/// <b>Cost.</b> One question is one use, however many tool rounds it takes; the rounds are capped
/// by <c>StaffAi:MaxToolRounds</c>.
/// </para>
/// </summary>
public sealed class AskHandler(
    IEnumerable<IStaffAiTool> tools,
    StaffAiGateway gateway,
    IStaffAiReader reader,
    ListBranchesHandler branches,
    BranchGuard branchGuard,
    GetClinicProfileHandler profile,
    ICurrentUser currentUser,
    RequestValidator validator,
    IAuditTrail audit,
    IClock clock,
    ILogger<AskHandler> logger)
{
    private const string GiveUp = "ขอโทษค่ะ ตอนนี้ตอบคำถามนี้ไม่ได้ ลองถามให้แคบลง หรือดูจากหน้ารายงานโดยตรง";

    public async Task<ChatReplyDto> HandleAsync(AskCommand command, CancellationToken cancellationToken = default)
    {
        gateway.EnsureEnabled();
        await validator.ValidateAsync(command, cancellationToken);

        var offered = tools
            .Where(tool => currentUser.Scopes.Allows(tool.RequiredScope))
            .ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        var pseudonymizer = new Pseudonymizer();
        foreach (var name in await reader.StaffNamesAsync(cancellationToken))
        {
            pseudonymizer.Person(name, PersonalKind.Staff);
        }

        var visible = branchGuard.VisibleBranchIds;
        var sites = (await branches.HandleAsync(cancellationToken))
            .Where(branch => visible is null || visible.Contains(branch.Id))
            .Select(branch => $"{branch.Id}: {branch.Name}")
            .ToList();

        var clinic = await profile.HandleAsync(cancellationToken);
        var system = AskPrompt.System(clinic.Name, clock.NowInClinicTime(), sites, [.. offered.Keys]);
        var declarations = offered.Values.Select(tool => new ModelTool(tool.Name, tool.Description, tool.ParametersSchema)).ToList();

        var messages = command.Messages
            .Select(message => message.Role == "user"
                ? ModelMessage.User(pseudonymizer.Mask(message.Text.Trim()))
                : ModelMessage.Assistant(pseudonymizer.Mask(message.Text.Trim())))
            .ToList();

        var reservation = await gateway.ReserveAsync(StaffAiFeatures.Ask, cancellationToken);
        var actions = new List<AssistantAction>();
        var used = new List<string>();
        var (inputTokens, outputTokens) = (0, 0);
        var maxRounds = Math.Clamp(gateway.Settings.MaxToolRounds, 0, 10);

        for (var round = 0; round <= maxRounds; round++)
        {
            var turn = await gateway.CallAsync(reservation, new ModelRequest(system, messages, declarations), answeredBefore: round > 0, cancellationToken);

            inputTokens += turn.InputTokens;
            outputTokens += turn.OutputTokens;

            if (turn.ToolCalls.Count == 0)
            {
                logger.LogInformation(
                    "Staff AI ask answered in {Rounds} round(s), tools [{Tools}], tokens in {In} out {Out}",
                    round + 1, string.Join(",", used), inputTokens, outputTokens);

                await AuditAsync(used, round + 1, cancellationToken);

                var reply = string.IsNullOrWhiteSpace(turn.Text) ? GiveUp : pseudonymizer.Restore(turn.Text.Trim());
                return new ChatReplyDto(reply, [.. actions.DistinctBy(action => action.Href).Take(3)]);
            }

            messages.Add(ModelMessage.Calls(turn.ToolCalls));

            foreach (var call in turn.ToolCalls)
            {
                used.Add(call.Name);
                var result = await RunAsync(offered, call, cancellationToken);

                actions.AddRange(result.Actions ?? []);
                messages.Add(ModelMessage.Result(call, pseudonymizer.Mask(JsonSerializer.Serialize(result.Data, StaffToolJson.Options))));
            }
        }

        logger.LogWarning("Staff AI ask gave up after {Rounds} tool rounds, tools [{Tools}]", maxRounds, string.Join(",", used));
        await AuditAsync(used, maxRounds + 1, cancellationToken);

        return new ChatReplyDto(GiveUp, []);
    }

    private static async Task<ToolResult> RunAsync(
        IReadOnlyDictionary<string, IStaffAiTool> offered,
        ModelToolCall call,
        CancellationToken cancellationToken)
    {
        // A tool the caller was not given is answered like a tool that does not exist: it never runs.
        if (!offered.TryGetValue(call.Name, out var tool))
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

        try
        {
            return await tool.InvokeAsync(arguments, cancellationToken);
        }
        catch (AppException refused)
        {
            // A branch the caller does not work at (403) or a span too long (422) is an answer the
            // model can pass on, in the use case's own words.
            return new ToolResult(new { error = refused.Message });
        }
    }

    private Task AuditAsync(List<string> used, int rounds, CancellationToken cancellationToken) =>
        audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.AiAsk,
                AuditResources.AiAssistant,
                "ask",
                Details: new Dictionary<string, object?>
                {
                    ["tools"] = used.Distinct(StringComparer.Ordinal).ToArray(),
                    ["rounds"] = rounds,
                }),
            cancellationToken);
}
