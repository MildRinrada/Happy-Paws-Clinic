using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary><c>POST /queue/displays/claim</c> — <c>{ "code": "ABCD-EFGH" }</c>.</summary>
public sealed record ClaimQueueDisplayCommand(string? Code);

/// <summary>
/// The TV's half of setting up a screen: the setup code shown in the console, traded once for the
/// display's token. Not in the API reference; see <c>docs/contract-extensions.md § 30</c>.
/// <para>
/// Anonymous, because the TV has no credential yet, and rate-limited per address. Every failure is
/// the same <c>422</c> at <c>code</c>: wrong, already used, past its 30 minutes, or revoked. The
/// code is eight characters from thirty, about 6.5 × 10^11 possibilities, live for half an hour.
/// </para>
/// <para>
/// The exchange is one conditional UPDATE: the code is cleared as the token hash is written, and
/// only while no token is set. Two TVs typing the same code at once get one token between them.
/// </para>
/// </summary>
public sealed class ClaimQueueDisplayHandler(
    IQueueDisplayRepository displays,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ClaimedQueueDisplayDto> HandleAsync(
        ClaimQueueDisplayCommand command,
        CancellationToken cancellationToken = default)
    {
        var code = QueueDisplay.NormaliseCode(command.Code) ?? throw Refused();
        var codeHash = QueueDisplay.Hash(code);
        var now = clock.UtcNow;

        var display = await displays.FindBySetupCodeHashAsync(codeHash, cancellationToken);

        if (display is null || !display.CanBeClaimedAt(now))
        {
            throw Refused();
        }

        var token = QueueDisplay.NewToken();
        var claimed = false;

        await unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            claimed = await displays.ClaimAsync(display.Id, codeHash, QueueDisplay.Hash(token), now, transaction);

            if (!claimed)
            {
                return;
            }

            audit.Record(new AuditEvent(
                AuditActions.QueueDisplayClaimed,
                AuditResources.QueueDisplay,
                display.Id,
                BranchId: display.BranchId,
                Details: new Dictionary<string, object?> { ["name"] = display.Name }));
        }, cancellationToken);

        if (!claimed)
        {
            throw Refused();
        }

        return new ClaimedQueueDisplayDto(display.Id, display.Name, display.BranchId, token, display.ExpiresAt);
    }

    private static ValidationFailedException Refused() =>
        new(new Dictionary<string, string[]>
        {
            ["code"] = ["รหัสตั้งจอไม่ถูกต้อง ใช้ไปแล้ว หรือหมดอายุ ขอรหัสใหม่จากหน้าจอห้องรอในระบบพนักงาน"],
        });
}
