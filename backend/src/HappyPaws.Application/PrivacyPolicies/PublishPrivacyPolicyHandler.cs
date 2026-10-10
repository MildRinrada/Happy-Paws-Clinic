using System.Globalization;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// <c>POST /staff/privacy-policy/versions/{id}/publish</c> (<c>settings:danger</c>, the owner). Not
/// in the API reference: see <c>docs/contract-extensions.md § 35</c>.
/// </summary>
/// <param name="VersionId">From the path: the draft to publish.</param>
/// <param name="Label">The label readers and consents will see, <c>2026-11</c>.</param>
/// <param name="EffectiveDate"><c>yyyy-MM-dd</c>, today or later in clinic time.</param>
/// <param name="LegalReviewDone">The owner's statement that a lawyer has reviewed the text.</param>
/// <param name="IfMatchVersion">Required: the draft's <c>version</c>, so what is published is what was read.</param>
public sealed record PublishPrivacyPolicyCommand(
    string VersionId,
    string? Label,
    string? EffectiveDate,
    bool LegalReviewDone,
    uint? IfMatchVersion = null);

/// <summary>
/// Makes a draft the clinic's notice. From this moment it is the current version: the public page
/// shows it, and every consent recorded from now on carries its label. The effective date is the
/// date the page prints; it is not a schedule, and the version is current from the moment of
/// publishing whatever date is given (<c>§ 35</c> says why).
/// <para>
/// Refused: a version already published (<c>409 state_conflict</c>), a label another published
/// version has (<c>409 privacy_policy_label_taken</c>), the label <c>2026-10</c> that the old
/// hard-coded notice used (<c>422</c>), and an effective date before today (<c>422</c>).
/// <c>If-Match</c> is required. Audited as <c>privacy_policy.published</c>.
/// </para>
/// </summary>
public sealed class PublishPrivacyPolicyHandler(
    IPrivacyPolicyRepository policies,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<StaffPrivacyPolicyVersionDto> HandleAsync(
        PublishPrivacyPolicyCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, clock.ClinicTimeZone).DateTime);
        var (label, effectiveDate) = Validate(command, today);

        PrivacyPolicyVersion? version = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            version = await policies.FindAsync(command.VersionId, token)
                ?? throw new NotFoundException("privacy_policy_version", command.VersionId);

            if (version.IsPublished)
            {
                throw new StateConflictException("privacy_policy_version", "published", "publish");
            }

            ConcurrencyGuard.EnsureRequired(version, command.IfMatchVersion);

            if (await policies.LabelTakenAsync(label, token))
            {
                throw LabelTaken(label);
            }

            var previous = await policies.CurrentAsync(token);

            version.Publish(label, effectiveDate, command.LegalReviewDone, staffId, now, today);

            audit.Record(new AuditEvent(
                AuditActions.PrivacyPolicyPublished,
                AuditResources.PrivacyPolicy,
                version.Id,
                Details: new Dictionary<string, object?>
                {
                    ["label"] = version.Label,
                    ["effective_date"] = version.EffectiveDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["legal_review_done"] = version.LegalReviewDone,
                    ["previous_version_id"] = previous?.Id,
                    ["previous_label"] = previous?.Label,
                    ["template_version"] = version.TemplateVersion,
                    ["section_count"] = version.Sections.Count,
                }));
        }, cancellationToken);

        return StaffPrivacyPolicyVersionDto.From(version!, version!.Id);
    }

    /// <summary>The answer when a published version already has the label. Also used for the race the unique index catches.</summary>
    public static ConflictException LabelTaken(string? label) =>
        new(ErrorCodes.PrivacyPolicyLabelTaken, "มีประกาศที่เผยแพร่แล้วใช้เลขฉบับนี้อยู่ กรุณาตั้งเลขฉบับใหม่")
        {
            MessageEn = "A published version already has this label.",
            Field = "label",
            Details = label is null ? null : new Dictionary<string, object> { ["label"] = label },
        };

    private static (string Label, DateOnly EffectiveDate) Validate(PublishPrivacyPolicyCommand command, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var label = command.Label?.Trim() ?? string.Empty;

        if (label.Length == 0)
        {
            errors["label"] = ["ต้องระบุเลขฉบับ"];
        }
        else if (!PrivacyPolicyRules.IsLabel(label))
        {
            errors["label"] =
                [$"เลขฉบับใช้ได้แค่ตัวอักษรภาษาอังกฤษ ตัวเลข จุด ขีดล่าง และขีดกลาง ขึ้นต้นด้วยตัวอักษรหรือตัวเลข ยาวไม่เกิน {PrivacyPolicyRules.LabelMaxLength} ตัว เช่น 2026-11"];
        }
        else if (label == PrivacyPolicyRules.LegacyLabel)
        {
            errors["label"] = ["เลขฉบับ 2026-10 ใช้กับประกาศเดิมที่เขียนไว้ในโค้ดหน้าเว็บแล้ว กรุณาใช้เลขอื่น"];
        }

        var effectiveDate = default(DateOnly);

        if (string.IsNullOrWhiteSpace(command.EffectiveDate))
        {
            errors["effective_date"] = ["ต้องระบุวันที่มีผล"];
        }
        else if (!DateOnly.TryParseExact(command.EffectiveDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out effectiveDate))
        {
            errors["effective_date"] = ["วันที่มีผลต้องอยู่ในรูป yyyy-MM-dd"];
        }
        else if (effectiveDate < today)
        {
            errors["effective_date"] = ["วันที่มีผลต้องเป็นวันนี้หรือหลังจากนี้"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        return (label, effectiveDate);
    }
}
