using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JodVyapar.Api.Features.Identity;

/// <summary>Bound from the "Jwt" section. Key comes from user-secrets / environment variables.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string Key { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 480;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}

/// <summary>Claim types used in our access tokens (short JWT names, not the long WS-* URIs).</summary>
public static class AuthClaims
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Email = JwtRegisteredClaimNames.Email;
    public const string Role = "role";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's Id from the "sub" claim, or null if it is missing or malformed.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(AuthClaims.UserId)?.Value, out var userId) ? userId : null;
}

public sealed class TokenService(IOptions<JwtOptions> options)
{
    public (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaims.UserId, user.Id.ToString()),
            new(AuthClaims.Name, user.FullName),
            new(AuthClaims.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        // One "role" claim per held role (Buyer, Seller and/or Admin).
        claims.AddRange(roles.Select(role => new Claim(AuthClaims.Role, role)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
        });

        return (token, expiresAt);
    }
}
