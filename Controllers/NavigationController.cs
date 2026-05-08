using Erp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/navigation")]
public sealed class NavigationController(AppDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet("side-nav")]
    public async Task<IReadOnlyList<SideNavItemResponse>> GetSideNav()
    {
        var roleIdClaim = User.FindFirst("role_id")?.Value;
        if (!int.TryParse(roleIdClaim, out var roleId))
        {
            return [];
        }

        var items = await db.SideNavItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Level)
            .ThenBy(item => item.DisplayOrder)
            .ToListAsync();

        var permissions = await db.RoleSideNavPermissions
            .AsNoTracking()
            .Where(permission => permission.RoleId == roleId)
            .ToDictionaryAsync(permission => permission.SideNavItemId);

        return BuildTree(items, parentId: null, permissions);
    }

    private static IReadOnlyList<SideNavItemResponse> BuildTree(
        IReadOnlyList<Models.SideNavItem> items,
        int? parentId,
        IReadOnlyDictionary<int, Models.RoleSideNavPermission> permissions)
    {
        return items
            .Where(item => item.ParentId == parentId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item =>
            {
                var children = BuildTree(items, item.Id, permissions);
                permissions.TryGetValue(item.Id, out var navPermission);
                var canShowItem = navPermission is { IsVisible: true } &&
                                  (navPermission.CanRead ||
                                   navPermission.CanWrite ||
                                   navPermission.CanUpdate ||
                                   navPermission.CanDelete);

                return canShowItem || children.Count > 0
                    ? new SideNavItemResponse(
                        item.Id,
                        item.ParentId,
                        item.Name,
                        item.Slug,
                        item.Path,
                        item.Permission,
                        item.Level,
                        item.DisplayOrder,
                        navPermission?.CanRead ?? false,
                        navPermission?.CanWrite ?? false,
                        navPermission?.CanUpdate ?? false,
                        navPermission?.CanDelete ?? false,
                        navPermission?.IsVisible ?? false,
                        children)
                    : null;
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }
}

public sealed record SideNavItemResponse(
    int Id,
    int? ParentId,
    string Name,
    string Slug,
    string Path,
    string Permission,
    int Level,
    int DisplayOrder,
    bool CanRead,
    bool CanWrite,
    bool CanUpdate,
    bool CanDelete,
    bool IsVisible,
    IReadOnlyList<SideNavItemResponse> Children);
