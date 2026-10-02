using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.ReplaceAssignmentRules;

/// <summary><c>PUT /staff/schedules/assignment-rules</c>. The whole set; an empty list removes every rule.</summary>
/// <param name="Rules">The rules in the order they are tried.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record ReplaceAssignmentRulesCommand(IReadOnlyList<AssignmentRule> Rules, uint? IfMatchVersion = null);

/// <summary>
/// Replaces the rules that pick a vet when a booking names none — "กติกาจ่ายเคสอัตโนมัติ เช่น
/// หมอแมวรับเคสแมวก่อน".
/// <para>
/// A <c>PUT</c> replaces the whole set, per <c>docs/assumptions.md § 6</c>. Every staff id must be
/// an active member and every service must exist: a rule that names someone who has left would
/// quietly stop doing anything.
/// </para>
/// <para>
/// Where the rules act: booking without a named vet, online and at the counter, and the substitutes
/// suggested when someone takes time off. The queue is still assigned by hand
/// (<c>POST /staff/queue/{id}/assign</c>).
/// </para>
/// </summary>
public sealed class ReplaceAssignmentRulesHandler(
    IClinicSettingsStore settings,
    IStaffRepository staff,
    IBookingCatalog catalog,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<AssignmentRulesDto> HandleAsync(
        ReplaceAssignmentRulesCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureReferencesExistAsync(command.Rules, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var rules = await settings.LoadForUpdateAsync(
                AssignmentRules.SingletonId, AssignmentRules.CreateDefault, token);

            ConcurrencyGuard.Ensure(rules, command.IfMatchVersion);

            var before = rules.Rules.Count;
            rules.Replace(command.Rules);

            audit.Record(new AuditEvent(
                AuditActions.AssignmentRulesReplaced,
                AuditResources.Settings,
                AssignmentRules.SingletonId,
                Details: new Dictionary<string, object?>
                {
                    ["rules_before"] = before,
                    ["rules_after"] = rules.Rules.Count,
                }));
        }, cancellationToken);

        return AssignmentRulesDto.From(
            await settings.ReadAsync(AssignmentRules.SingletonId, AssignmentRules.CreateDefault, cancellationToken));
    }

    private async Task EnsureReferencesExistAsync(IReadOnlyList<AssignmentRule> rules, CancellationToken cancellationToken)
    {
        var staffIds = rules.SelectMany(rule => rule.StaffIds).Distinct(StringComparer.Ordinal).ToArray();
        var missing = await staff.MissingAsync(staffIds, cancellationToken);

        if (missing.Count > 0)
        {
            throw Invalid("staff_ids", $"ไม่พบพนักงานที่ยังทำงานอยู่: {string.Join(", ", missing)}");
        }

        foreach (var serviceId in rules.Select(rule => rule.ServiceId).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            if (await catalog.FindServiceAsync(serviceId.Trim(), cancellationToken) is null)
            {
                throw Invalid("service_id", $"ไม่พบบริการ {serviceId}");
            }
        }
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
