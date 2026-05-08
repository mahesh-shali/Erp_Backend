using Erp.Api.Data;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController(AppDbContext db) : ControllerBase
{
    [Authorize(Policy = AppPermissions.RolesView)]
    [HttpGet]
    public async Task<IReadOnlyList<RoleResponse>> GetAll()
    {
        return await db.Roles
            .Include(role => role.Permissions)
            .OrderBy(role => role.Name)
            .Select(role => new RoleResponse(
                role.Id,
                role.Name,
                role.Description,
                role.CreatedDate,
                role.ModifiedDate,
                role.Permissions.Select(permission => permission.Permission).Order()))
            .ToListAsync();
    }
}

public sealed record RoleResponse(
    int Id,
    string Name,
    string Description,
    DateTimeOffset CreatedDate,
    DateTimeOffset? ModifiedDate,
    IEnumerable<string> Permissions);
