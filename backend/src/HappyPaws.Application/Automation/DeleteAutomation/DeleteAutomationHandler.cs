using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Automation.DeleteAutomation;

/// <summary>
/// <c>DELETE /staff/automations/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 19</c>.
/// </summary>
/// <param name="AutomationId">The rule.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record DeleteAutomationCommand(string AutomationId, uint? IfMatchVersion = null);

/// <summary>
/// Deletes a reminder rule: it stops sending, and it leaves every list.
/// <para>
/// <b>The history stays.</b> The row is marked deleted, not removed. Its "already sent" records
/// (<c>automation_dispatches</c>) point at it, and each reminder it sent is a row in the delivery
/// log with the dispatch as its batch, so "who sent this owner that message last month" is still
/// answered by name after the rule is gone. Removing the row would mean removing that history with
/// it, or leaving it pointing at nothing.
/// </para>
/// <para>
/// A run that had already loaded the rule when it was deleted may finish its tick; the next tick
/// does not see it. The same is true of switching a rule off.
/// </para>
/// </summary>
public sealed class DeleteAutomationHandler(
    AutomationAccess access,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(DeleteAutomationCommand command, CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var rule = await access.LoadForChangeAsync(command.AutomationId, token);

            ConcurrencyGuard.Ensure(rule, command.IfMatchVersion);

            var wasEnabled = rule.Enabled;
            rule.Delete(staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.AutomationDeleted,
                AuditResources.Automation,
                rule.Id,
                BranchId: rule.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = rule.Name,
                    ["trigger"] = AutomationWire.ToWire(rule.Trigger),
                    ["was_enabled"] = wasEnabled,
                }));
        }, cancellationToken);
    }
}
