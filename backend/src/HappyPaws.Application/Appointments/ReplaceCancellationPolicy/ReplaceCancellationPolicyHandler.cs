using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Appointments.ReplaceCancellationPolicy;

/// <summary>
/// <c>PUT /staff/policies/cancellation</c> — "cutoff ค่าปรับ และจำนวนครั้งที่ยอมให้". A <c>PUT</c>,
/// so every field is sent; a missing one takes the default rather than the value before.
/// </summary>
public sealed record ReplaceCancellationPolicyCommand(
    int CutoffHours,
    long FeeSatang,
    int FreeAllowance,
    int WindowDays,
    uint? IfMatchVersion = null);

/// <summary>
/// Replaces the cancellation policy.
/// <para>
/// Audited with the before and after, because the policy decides what a customer can be asked to
/// pay. A change applies from now on: cancellations already marked late stay late, and strikes
/// already counted are counted again under the new window and allowance the next time anyone asks.
/// </para>
/// </summary>
public sealed class ReplaceCancellationPolicyHandler(
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<CancellationPolicyDto> HandleAsync(
        ReplaceCancellationPolicyCommand command,
        CancellationToken cancellationToken = default)
    {
        CancellationPolicy? saved = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var policy = await settings.CancellationPolicyForUpdateAsync(token);
            ConcurrencyGuard.Ensure(policy, command.IfMatchVersion);

            var before = CancellationPolicyDto.From(policy);

            policy.Replace(command.CutoffHours, command.FeeSatang, command.FreeAllowance, command.WindowDays);

            audit.Record(new AuditEvent(
                AuditActions.CancellationPolicyChanged,
                AuditResources.Settings,
                policy.Id,
                Details: new Dictionary<string, object?>
                {
                    ["before"] = Values(before),
                    ["after"] = Values(CancellationPolicyDto.From(policy)),
                }));

            saved = policy;
        }, cancellationToken);

        return CancellationPolicyDto.From(saved!);
    }

    private static Dictionary<string, object?> Values(CancellationPolicyDto policy) => new()
    {
        ["cutoff_hours"] = policy.CutoffHours,
        ["fee_satang"] = policy.FeeSatang,
        ["free_allowance"] = policy.FreeAllowance,
        ["window_days"] = policy.WindowDays,
    };
}
