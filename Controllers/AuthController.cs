using Erp.Api.Caching;
using Erp.Api.Auth;
using Erp.Api.Data;
using Erp.Api.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    CacheService cache,
    PasswordHasher<AppUser> passwordHasher,
    TokenService tokenService,
    IOptions<GoogleAuthOptions> googleOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(user => user.Email == request.Email))
        {
            return BadRequest("Email is already registered.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(existing => existing.Name == "Employee");
        if (role is null)
        {
            return BadRequest("Default Employee role has not been seeded yet.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            RoleId = role.Id
        };
        user.Password = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await cache.RemoveAsync(CacheKeys.Users);

        return Ok(await CreateSessionAsync(user.Id));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(existing => existing.Email == request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(await CreateSessionAsync(user.Id));
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var clientId = googleOptions.Value.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google login is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("Invalid Google credential.");
        }

        if (payload.EmailVerified is not true || string.IsNullOrWhiteSpace(payload.Email))
        {
            return Unauthorized("Google email must be verified.");
        }

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(existing => existing.Email == email);
        if (user is null)
        {
            var role = await db.Roles.FirstOrDefaultAsync(existing => existing.Name == "Employee");
            if (role is null)
            {
                return BadRequest("Default Employee role has not been seeded yet.");
            }

            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name.Trim(),
                Email = email,
                RoleId = role.Id
            };
            user.Password = passwordHasher.HashPassword(user, TokenService.GenerateRefreshToken());
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await cache.RemoveAsync(CacheKeys.Users);
        }

        return Ok(await CreateSessionAsync(user.Id));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var tokenHash = TokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await db.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user!.Role)
            .ThenInclude(role => role!.Permissions)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (refreshToken is null || !refreshToken.IsActive || refreshToken.User is null)
        {
            return Unauthorized("Invalid refresh token.");
        }

        refreshToken.RevokedDate = DateTimeOffset.UtcNow;
        return Ok(await CreateSessionAsync(refreshToken.UserId, refreshToken));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var tokenHash = TokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
        if (refreshToken is not null)
        {
            refreshToken.RevokedDate = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await LoadUserAsync(userId);
        return user is null ? Unauthorized() : Ok(tokenService.CreateToken(user));
    }

    private async Task<AuthResponse> CreateSessionAsync(Guid userId, RefreshToken? replacedToken = null)
    {
        var user = await LoadUserAsync(userId) ?? throw new InvalidOperationException("User was not found.");
        var refreshTokenValue = TokenService.GenerateRefreshToken();
        var refreshTokenHash = TokenService.HashRefreshToken(refreshTokenValue);

        if (replacedToken is not null)
        {
            replacedToken.ReplacedByTokenHash = refreshTokenHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedDate = DateTimeOffset.UtcNow,
            ExpiresDate = DateTimeOffset.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var response = tokenService.CreateToken(user);
        return response with { RefreshToken = refreshTokenValue };
    }

    private Task<AppUser?> LoadUserAsync(Guid userId)
    {
        return db.Users
            .Include(user => user.Role)
            .ThenInclude(role => role!.Permissions)
            .FirstOrDefaultAsync(user => user.Id == userId);
    }
}

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record GoogleLoginRequest(string Credential);
public sealed record RefreshRequest(string RefreshToken);
