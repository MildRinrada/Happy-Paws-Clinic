using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// <c>POST /staff/privacy-policy/versions</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 35</c>.
/// </summary>
/// <param name="Source">
/// <c>template</c> (the text that ships in the code), <c>current</c> (the current notice) or
/// <c>version</c> (any version by <paramref name="VersionId"/>, drafts included).
/// </param>
/// <param name="VersionId">The version to copy, for <c>version</c>.</param>
public sealed record CreatePrivacyPolicyDraftCommand(string? Source, string? VersionId = null);

/// <summary>
/// Makes a new draft. A draft is never public; publishing is its own step, for the owner. The
/// usual way to change the notice is a draft copied from the current one: the current one itself
/// can never be edited. Audited as <c>privacy_policy.draft_created</c>.
/// </summary>
public sealed class CreatePrivacyPolicyDraftHandler(
    IPrivacyPolicyRepository policies,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<StaffPrivacyPolicyVersionDto> HandleAsync(
        CreatePrivacyPolicyDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var source = command.Source?.Trim().ToLowerInvariant();

        if (source is not ("template" or "current" or "version"))
        {
            throw Invalid("source", "source ต้องเป็น template, current หรือ version");
        }

        if (source == "version" && string.IsNullOrWhiteSpace(command.VersionId))
        {
            throw Invalid("version_id", "ต้องระบุ version_id ของฉบับที่จะคัดลอก");
        }

        PrivacyPolicyVersion? draft = null;
        string? currentId = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var current = await policies.CurrentAsync(token);
            currentId = current?.Id;

            draft = source switch
            {
                "template" => PrivacyPolicyVersion.DraftFromTemplate(staffId),
                "current" => PrivacyPolicyVersion.DraftFrom(
                    current ?? throw Invalid("source", "คลินิกยังไม่ได้เผยแพร่ประกาศฉบับใด ให้สร้างร่างจาก template หรือจากร่างที่มีอยู่"),
                    staffId),
                _ => PrivacyPolicyVersion.DraftFrom(
                    await policies.FindAsync(command.VersionId!.Trim(), token)
                        ?? throw new NotFoundException("privacy_policy_version", command.VersionId),
                    staffId),
            };

            policies.Add(draft);

            audit.Record(new AuditEvent(
                AuditActions.PrivacyPolicyDraftCreated,
                AuditResources.PrivacyPolicy,
                draft.Id,
                Details: new Dictionary<string, object?>
                {
                    ["source"] = source,
                    ["based_on_version_id"] = draft.BasedOnVersionId,
                    ["template_version"] = draft.TemplateVersion,
                    ["created_from"] = WireEnum.Of(draft.CreatedFrom),
                    ["section_count"] = draft.Sections.Count,
                }));
        }, cancellationToken);

        return StaffPrivacyPolicyVersionDto.From(draft!, currentId);
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
