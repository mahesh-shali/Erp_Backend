using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Erp.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Erp.Api.Auth;

public sealed class TokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AuthResponse CreateToken(AppUser user)
    {
        var roleName = user.Role?.Name ?? string.Empty;
        var permissions = user.Role?.Permissions.Select(permission => permission.Permission).Order().ToArray() ?? [];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, roleName),
            new("role_id", user.RoleId.ToString())
        };

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            string.Empty,
            expiresAt,
            user.Name,
            user.Email,
            [roleName],
            permissions);
    }

    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string DisplayName,
    string Email,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions);
