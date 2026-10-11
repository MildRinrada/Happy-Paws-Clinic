using HappyPaws.Application.Clinics;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.GetAssignmentRules;

/// <summary>
/// <c>GET /staff/schedules/assignment-rules</c>: the rules as stored, in the order they are tried,
/// with the version to send as <c>If-Match</c> on the next <c>PUT</c>. The same shape the
/// <c>PUT</c> answers. Not in the API reference; see <c>docs/contract-extensions.md § 48</c>.
/// <para>
/// A clinic that has never saved any reads an empty list with version 0, which the <c>PUT</c> also
/// accepts as <c>If-Match</c> before the first save.
/// </para>
/// </summary>
public sealed class GetAssignmentRulesHandler(IClinicSettingsStore settings)
{
    public async Task<AssignmentRulesDto> HandleAsync(CancellationToken cancellationToken = default) =>
        AssignmentRulesDto.From(
            await settings.ReadAsync(AssignmentRules.SingletonId, AssignmentRules.CreateDefault, cancellationToken));
}
