using Erp.Api.Caching;
using Erp.Api.Data;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(AppDbContext db, CacheService cache) : ControllerBase
{
    [Authorize(Policy = AppPermissions.UsersView)]
    [HttpGet]
    public async Task<IReadOnlyList<UserResponse>> GetAll()
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.Users,
            async () => await db.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .OrderBy(user => user.Email)
                .Select(user => new UserResponse(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.RoleId,
                    user.Role == null ? string.Empty : user.Role.Name))
                .ToListAsync(),
            TimeSpan.FromMinutes(2));
    }
}

public sealed record UserResponse(Guid Id, string Name, string Email, int RoleId, string Role);
