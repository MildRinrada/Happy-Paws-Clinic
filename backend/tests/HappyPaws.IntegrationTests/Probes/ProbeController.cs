using HappyPaws.Api.Authorization;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppErrors = HappyPaws.Application.Common.Errors;

namespace HappyPaws.IntegrationTests.Probes;

/// <summary>
/// Test-only endpoints that exercise the cross-cutting pipeline.
/// <para>
/// This controller lives in the test assembly and is never part of the shipped API. It exists so
/// that authentication, scope enforcement, the 2FA gate, the error envelope and idempotency can be
/// proven end-to-end <b>before</b> the first real controller is written — rather than discovering
/// a broken claim name after 160 endpoints have been built on top of it.
/// </para>
/// </summary>
[ApiController]
[Route("_probe")]
public sealed class ProbeController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() => Ok(new { ok = true });

    /// <summary>Projects the caller as the Application layer sees them — proves the token round-trip.</summary>
    [HttpGet("identity")]
    public IActionResult Identity([FromServices] ICurrentUser currentUser) => Ok(new
    {
        user_id = currentUser.UserId,
        staff_id = currentUser.StaffId,
        customer_id = currentUser.CustomerId,
        branches = currentUser.BranchIds,
        scopes = currentUser.Scopes.ToArray(),
        two_factor = currentUser.TwoFactorSatisfied,
        authenticated = currentUser.IsAuthenticated,
    });

    [HttpGet("scoped")]
    [RequireScope(Scopes.Appointment.Read)]
    public IActionResult Scoped() => Ok(new { ok = true });

    /// <summary>Guarded by a medical scope, so the 2FA requirement applies.</summary>
    [HttpGet("sensitive")]
    [RequireScope(Scopes.Record.Sign)]
    public IActionResult Sensitive() => Ok(new { ok = true });

    [HttpGet("branch/{branchId}")]
    public IActionResult Branch(string branchId, [FromServices] BranchGuard guard)
    {
        guard.EnsureCanAccess(branchId);
        return Ok(new { ok = true });
    }

    /// <summary>Returns a fresh value each execution, so a replayed response is detectable.</summary>
    [HttpPost("idempotent")]
    [Idempotent]
    public IActionResult Idempotent() => Ok(new { token = Guid.NewGuid().ToString("N") });

    [HttpPost("idempotent-failure")]
    [Idempotent]
    public IActionResult IdempotentFailure() =>
        throw new InvalidOperationException("probe failure");

    [HttpGet("throw/{kind}")]
    [AllowAnonymous]
    public IActionResult Throw(string kind) => throw Build(kind);

    private static Exception Build(string kind) => kind switch
    {
        "validation" => new AppErrors.ValidationFailedException(
            new Dictionary<string, string[]> { ["starts_at"] = ["ต้องไม่เป็นอดีต"] }),

        "not-found" => new AppErrors.NotFoundException("appointment", "apt_secret"),

        "forbidden" => new AppErrors.ForbiddenException("probe reason that must not leak"),

        "insufficient-scope" => new AppErrors.InsufficientScopeException([Scopes.Record.Sign]),

        "version-conflict" => new AppErrors.VersionConflictException("Appointment", 7, 9),

        "record-locked" => new RecordLockedException("rec_123"),

        "state-conflict" => new StateConflictException("Appointment", "completed", "cancel"),

        _ => new InvalidOperationException("probe internal failure with a secret in the message"),
    };
}
