using System.Security.Claims;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Access;

namespace HappyPaws.Api.Http;

/// <summary>
/// Reads the authenticated caller off the validated access token.
/// <para>
/// This is the only place <c>HttpContext.User</c> is translated into the shape the Application
/// layer sees. Everything downstream depends on <see cref="ICurrentUser"/>, so a use case cannot
/// reach around it to a claim the authorization layer did not check.
/// </para>
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? UserId => Claim(ClinicClaimTypes.Subject);

    public string? StaffId => Claim(ClinicClaimTypes.StaffId);

    public string? CustomerId => Claim(ClinicClaimTypes.CustomerId);

    public string? SessionId => Claim(ClinicClaimTypes.SessionId);

    public string? ApiKeyId => Claim(ClinicClaimTypes.ApiKey);

    public string? DisplayId => Claim(ClinicClaimTypes.Display);

    public IReadOnlyCollection<string> BranchIds => Claims(ClinicClaimTypes.Branch);

    public ScopeSet Scopes => new(Claims(ClinicClaimTypes.Scope));

    public bool TwoFactorSatisfied =>
        string.Equals(Claim(ClinicClaimTypes.TwoFactorSatisfied), "1", StringComparison.Ordinal);

    private string? Claim(string type) => Principal?.FindFirst(type)?.Value;

    private string[] Claims(string type) =>
        Principal is null ? [] : [.. Principal.FindAll(type).Select(claim => claim.Value)];
}
