using Erp.Api.Permissions;
using Erp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController(AppDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IReadOnlyList<string> GetAll()
    {
        return AppPermissions.All;
    }

    [Authorize]
    [HttpGet("navigation")]
    public async Task<IReadOnlyList<NavigationPermissionResponse>> GetNavigationPermissions()
    {
        return await db.SideNavItems
            .AsNoTracking()
            .OrderBy(item => item.Level)
            .ThenBy(item => item.DisplayOrder)
            .Select(item => new NavigationPermissionResponse(
                item.Id,
                item.ParentId,
                item.Name,
                item.Permission,
                item.Level))
            .ToListAsync();
    }
}

public sealed record NavigationPermissionResponse(int Id, int? ParentId, string Name, string Permission, int Level);
