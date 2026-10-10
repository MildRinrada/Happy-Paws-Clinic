using System.Globalization;
using System.Text;
using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Messaging.Dtos;
using HappyPaws.Application.Messaging.ListThreads;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.StaffAi.Privacy;
using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Messaging;

namespace HappyPaws.Application.StaffAi.OwnerMessage;

/// <summary>
/// <c>POST /staff/ai/drafts/owner-message</c>: <c>{ "kind": "aftercare", "consultation_id": "..." }</c>
/// or <c>{ "kind": "thread_reply", "thread_id": "..." }</c>.
/// </summary>
public sealed record DraftOwnerMessageCommand(string? Kind, string? ConsultationId, string? ThreadId);

public sealed class DraftOwnerMessageValidator : AbstractValidator<DraftOwnerMessageCommand>
{
    public DraftOwnerMessageValidator()
    {
        RuleFor(command => command.Kind)
            .Must(kind => kind is DraftOwnerMessageHandler.Aftercare or DraftOwnerMessageHandler.ThreadReply)
            .WithMessage($"kind ต้องเป็น {DraftOwnerMessageHandler.Aftercare} หรือ {DraftOwnerMessageHandler.ThreadReply}");

        When(command => command.Kind == DraftOwnerMessageHandler.Aftercare, () =>
            RuleFor(command => command.ConsultationId)
                .Must(id => !string.IsNullOrWhiteSpace(id) && id.Length <= 40)
                .WithMessage("ต้องระบุ consultation_id ของเคส"));

        When(command => command.Kind == DraftOwnerMessageHandler.ThreadReply, () =>
            RuleFor(command => command.ThreadId)
                .Must(id => !string.IsNullOrWhiteSpace(id) && id.Length <= 40)
                .WithMessage("ต้องระบุ thread_id ของบทสนทนา"));
    }
}

/// <summary>The drafted words. Nothing was sent.</summary>
public sealed record OwnerMessageDraftDto(string Text);

/// <summary>
/// Drafts words for an owner: aftercare instructions from a case, or a reply in a customer thread.
/// <para>
/// <b>Gate.</b> <c>message:write</c> and <c>ai:use</c> at the route. Each kind also needs the right
/// to read what it is drafted from, checked here: an aftercare draft holds the case's diagnoses and
/// medicines, so it needs <c>consult:read</c> and the case's branch; a reply needs
/// <c>message:read</c> and the thread's branch, the same reach as reading the thread.
/// </para>
/// <para>
/// <b>Names.</b> The owner is <c>[OWNER_1]</c> to the model and is put back in the draft, so the
/// greeting has their real name. Staff in a thread are <c>[STAFF_n]</c>. Phone numbers and emails
/// typed into messages or notes are masked by pattern.
/// </para>
/// </summary>
public sealed class DraftOwnerMessageHandler(
    ConsultationAccess consultations,
    IPetRepository pets,
    ListThreadsHandler threads,
    IStaffAiReader reader,
    StaffAiGateway gateway,
    GetClinicProfileHandler profile,
    ICurrentUser currentUser,
    ClinicDays days,
    RequestValidator validator,
    IAuditTrail audit)
{
    public const string Aftercare = "aftercare";
    public const string ThreadReply = "thread_reply";

    /// <summary>How many of the thread's newest messages the reply is drafted from.</summary>
    public const int ThreadMessages = 12;

    /// <summary>A long aftercare sheet is a sign the model went beyond the case.</summary>
    public const int AftercareMax = 3000;

    public async Task<OwnerMessageDraftDto> HandleAsync(DraftOwnerMessageCommand command, CancellationToken cancellationToken = default)
    {
        gateway.EnsureEnabled();
        await validator.ValidateAsync(command, cancellationToken);

        return command.Kind == Aftercare
            ? await AftercareAsync(command.ConsultationId!.Trim(), cancellationToken)
            : await ReplyAsync(command.ThreadId!.Trim(), cancellationToken);
    }

    private async Task<OwnerMessageDraftDto> AftercareAsync(string consultationId, CancellationToken cancellationToken)
    {
        currentUser.RequireScopes(Scopes.Consult.Read);

        var consultation = await consultations.ForStaffAsync(consultationId, cancellationToken);
        var pet = await pets.FindByIdAsync(consultation.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", consultation.PetId);

        var pseudonymizer = await OwnerFirstAsync(consultation.CustomerId, null, cancellationToken);
        var data = CaseData(consultation, PatientDescription.Of(pet, days.Today), pseudonymizer);
        var clinic = await profile.HandleAsync(cancellationToken);

        var answer = await gateway.CompleteTextAsync(
            StaffAiFeatures.OwnerMessage,
            OwnerMessagePrompt.Aftercare(clinic.Name),
            StaffAiPromptRules.Fence(data),
            cancellationToken);

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.AiOwnerMessageDraft,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?> { ["kind"] = Aftercare }),
            cancellationToken);

        return new OwnerMessageDraftDto(Cut(Greeting(pseudonymizer.Restore(answer)), AftercareMax));
    }

    private async Task<OwnerMessageDraftDto> ReplyAsync(string threadId, CancellationToken cancellationToken)
    {
        currentUser.RequireScopes(Scopes.Message.Read);

        // The thread as the inbox reads it: branch-checked by the same use case, newest messages.
        var page = await threads.HandleAsync(
            new ListThreadsQuery(ThreadId: threadId, Messages: ThreadMessages, Page: new CursorPageRequest(1, null)),
            cancellationToken);

        var thread = page.Data.FirstOrDefault(found => found.Id == threadId)
            ?? throw new NotFoundException("thread", threadId);

        var pseudonymizer = await OwnerFirstAsync(thread.CustomerId, thread.CustomerName, cancellationToken);
        var clinic = await profile.HandleAsync(cancellationToken);

        var answer = await gateway.CompleteTextAsync(
            StaffAiFeatures.OwnerMessage,
            OwnerMessagePrompt.ThreadReply(clinic.Name, ThreadMessage.ReplyTextMax),
            StaffAiPromptRules.Fence(Conversation(thread, pseudonymizer)),
            cancellationToken);

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.AiOwnerMessageDraft,
                AuditResources.MessageThread,
                thread.Id,
                BranchId: thread.BranchId,
                Details: new Dictionary<string, object?> { ["kind"] = ThreadReply, ["messages"] = thread.Messages.Count }),
            cancellationToken);

        return new OwnerMessageDraftDto(Cut(Greeting(pseudonymizer.Restore(answer)), ThreadMessage.ReplyTextMax));
    }

    /// <summary>The owner is registered first so they are always <c>[OWNER_1]</c>, as the prompts say.</summary>
    private async Task<Pseudonymizer> OwnerFirstAsync(string customerId, string? knownName, CancellationToken cancellationToken)
    {
        var pseudonymizer = new Pseudonymizer();
        var owner = await reader.CustomerAsync(customerId, cancellationToken);

        pseudonymizer.Person(owner?.Name ?? knownName ?? "ลูกค้า", PersonalKind.Owner);
        pseudonymizer.Person(knownName, PersonalKind.Owner);
        pseudonymizer.Register(owner?.Phone, PersonalKind.Phone);
        pseudonymizer.Register(owner?.Email, PersonalKind.Email);

        return pseudonymizer;
    }

    private static string CaseData(Consultation consultation, string patient, Pseudonymizer p)
    {
        var data = new StringBuilder();
        data.AppendLine(patient);
        data.AppendLine(CultureInfo.InvariantCulture, $"Visit date: {consultation.OpenedAt.ToOffset(TimeSpan.FromHours(7)):yyyy-MM-dd}");

        if (consultation.ChiefComplaint is { Length: > 0 } complaint)
        {
            data.AppendLine(CultureInfo.InvariantCulture, $"Reason for visit: {Line(p.Mask(complaint))}");
        }

        data.AppendLine("Diagnoses:");
        foreach (var diagnosis in consultation.Diagnoses.OrderByDescending(diagnosis => diagnosis.IsPrimary))
        {
            data.AppendLine(CultureInfo.InvariantCulture, $"- {Line(p.Mask(diagnosis.Description))}{(diagnosis.Note is { Length: > 0 } note ? $" ({Line(p.Mask(note))})" : string.Empty)}");
        }

        if (consultation.Diagnoses.Count == 0)
        {
            data.AppendLine("- none recorded");
        }

        data.AppendLine("Treatments done at the clinic:");
        foreach (var treatment in consultation.Treatments.OrderBy(treatment => treatment.PerformedAt))
        {
            data.AppendLine(CultureInfo.InvariantCulture, $"- {Line(p.Mask(treatment.Name))} x{treatment.Quantity}{(treatment.Note is { Length: > 0 } note ? $" ({Line(p.Mask(note))})" : string.Empty)}");
        }

        if (consultation.Treatments.Count == 0)
        {
            data.AppendLine("- none recorded");
        }

        data.AppendLine("Prescriptions:");
        foreach (var medicine in consultation.Prescriptions.OrderBy(medicine => medicine.PrescribedAt))
        {
            data.AppendLine(CultureInfo.InvariantCulture,
                $"- {Line(medicine.DrugName)}, dose {Line(medicine.Dose)}, {Line(medicine.Frequency)}, for {medicine.DurationDays} days{(medicine.Instructions is { Length: > 0 } instructions ? $", instructions: {Line(p.Mask(instructions))}" : string.Empty)}");
        }

        if (consultation.Prescriptions.Count == 0)
        {
            data.AppendLine("- none");
        }

        if (consultation.Plan is { Length: > 0 } plan)
        {
            data.AppendLine(CultureInfo.InvariantCulture, $"Vet's plan: {Line(p.Mask(plan))}");
        }

        return data.ToString();
    }

    private static string Conversation(ThreadDto thread, Pseudonymizer p)
    {
        var data = new StringBuilder();
        data.AppendLine("Conversation, oldest first. \"customer\" is [OWNER_1]; \"clinic\" is the clinic's staff.");

        // Every staff name first, so one named in an earlier message is masked there too.
        foreach (var name in thread.Messages.Select(message => message.SentByName).Distinct(StringComparer.Ordinal))
        {
            p.Person(name, PersonalKind.Staff);
        }

        foreach (var message in thread.Messages)
        {
            var who = message.Direction == "inbound"
                ? "customer"
                : $"clinic{(p.Person(message.SentByName, PersonalKind.Staff) is { } staff ? " " + staff : string.Empty)}";

            var words = message.Kind == "text" || !string.IsNullOrWhiteSpace(message.Text)
                ? Line(p.Mask(message.Text ?? string.Empty))
                : $"(sent a {message.Kind})";

            data.AppendLine(CultureInfo.InvariantCulture, $"{who} ({message.SentAt.ToOffset(TimeSpan.FromHours(7)):yyyy-MM-dd HH:mm}): {words}");
        }

        if (thread.Messages.Count == 0)
        {
            data.AppendLine("(no messages)");
        }

        return data.ToString();
    }

    /// <summary>
    /// The prompt writes "คุณ[OWNER_1]", and many records already store the name as "คุณสมชาย":
    /// the doubled title is folded back to one.
    /// </summary>
    private static string Greeting(string text) => text.Replace("คุณคุณ", "คุณ", StringComparison.Ordinal);

    private static string Line(string text) =>
        string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string Cut(string text, int max) => text.Length <= max ? text.Trim() : text[..max].Trim();
}
