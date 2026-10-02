using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Customers.GetConsents;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.RecordConsent;

/// <summary>
/// <c>PUT /portal/consents</c> and <c>POST /staff/customers/{id}/consents</c>.
/// <para>
/// <c>Decisions</c> maps the wire purpose name to the new position, so one call can record several
/// toggles moved together on one screen.
/// </para>
/// </summary>
public sealed record RecordConsentCommand(
    string CustomerId,
    IReadOnlyDictionary<string, bool> Decisions,
    ConsentSource Source,
    string? PolicyVersion = null,
    string? Note = null);

/// <summary>
/// Records consent decisions as new rows, never as edits.
/// <para>
/// A decision that repeats the current position is dropped rather than stored: a portal screen
/// posts every toggle it shows, and keeping the unchanged ones would bury the two rows that matter
/// under a stream of rows that record nothing. What is <b>not</b> dropped is a decision that
/// repeats a <i>default</i> — "they were asked and said yes" is a materially different fact from
/// "nobody ever asked", and it is the first one a clinic needs to be able to prove.
/// </para>
/// <para>
/// Every row is audited, granted and withdrawn under separate actions, because the two are looked
/// for separately when a complaint arrives.
/// </para>
/// </summary>
public sealed class RecordConsentHandler(
    IConsentRepository consents,
    GetConsentsHandler summary,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsentSummaryDto> HandleAsync(
        RecordConsentCommand command,
        CancellationToken cancellationToken = default)
    {
        var decisions = Parse(command.Decisions);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var current = await consents.CurrentAsync(command.CustomerId, token);

            foreach (var (purpose, granted) in decisions)
            {
                var recorded = current.GetValueOrDefault(purpose);

                if (recorded is not null && recorded.Granted == granted)
                {
                    continue;
                }

                var consent = CustomerConsent.Record(
                    command.CustomerId,
                    purpose,
                    granted,
                    command.Source,
                    clock.UtcNow,
                    command.Source == ConsentSource.Counter ? currentUser.UserId : null,
                    command.PolicyVersion,
                    command.Note);

                consents.Add(consent);

                audit.Record(new AuditEvent(
                    granted ? AuditActions.ConsentRecorded : AuditActions.ConsentWithdrawn,
                    AuditResources.Consent,
                    consent.Id,
                    Details: new Dictionary<string, object?>
                    {
                        ["customer_id"] = command.CustomerId,
                        ["purpose"] = ConsentStateDto.ToWire(purpose),
                        ["granted"] = granted,
                        ["source"] = command.Source.ToString().ToLowerInvariant(),
                    }));
            }
        }, cancellationToken);

        return await summary.HandleAsync(new GetConsentsQuery(command.CustomerId), cancellationToken);
    }

    private static Dictionary<ConsentPurpose, bool> Parse(IReadOnlyDictionary<string, bool> decisions)
    {
        if (decisions.Count == 0)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["consents"] = ["ต้องระบุความยินยอมอย่างน้อยหนึ่งรายการ"],
            });
        }

        var parsed = new Dictionary<ConsentPurpose, bool>();

        foreach (var (name, granted) in decisions)
        {
            var purpose = ConsentStateDto.ParseWire(name)
                ?? throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["consents"] = [$"ไม่รู้จักวัตถุประสงค์: {name}"],
                });

            parsed[purpose] = granted;
        }

        return parsed;
    }
}
