using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HappyPaws.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HappyPaws.Infrastructure.Identity;

/// <summary>
/// Issues the contract's 15-minute access tokens, signed with this instance's own key.
/// </summary>
public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, IClock clock) : IAccessTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public IssuedToken Issue(AccessTokenRequest request)
    {
        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(ClinicClaimTypes.Subject, request.UserId),
            new(ClinicClaimTypes.SessionId, request.SessionId),

            // Without jti, two tokens minted for the same session inside one second are
            // byte-identical, because every other claim and the second-resolution timestamps
            // match. That makes an individual token impossible to identify in a log or to
            // deny-list later. A unique id costs nothing and keeps both options open.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        if (request.StaffId is { Length: > 0 } staffId)
        {
            claims.Add(new Claim(ClinicClaimTypes.StaffId, staffId));
        }

        if (request.CustomerId is { Length: > 0 } customerId)
        {
            claims.Add(new Claim(ClinicClaimTypes.CustomerId, customerId));
        }

        claims.AddRange(request.BranchIds.Select(branch => new Claim(ClinicClaimTypes.Branch, branch)));
        claims.AddRange(request.Scopes.Select(scope => new Claim(ClinicClaimTypes.Scope, scope)));

        // Only ever stamped when 2FA actually happened. Endpoints guarding medical scopes read it,
        // so a token from /auth/staff/login alone must not carry it.
        if (request.TwoFactorSatisfied)
        {
            claims.Add(new Claim(ClinicClaimTypes.TwoFactorSatisfied, "1"));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
