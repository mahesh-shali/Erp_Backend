namespace Erp.Api.Models;

public sealed class RoleSideNavPermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int SideNavItemId { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool IsVisible { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedDate { get; set; }

    public AppRole? Role { get; set; }
    public SideNavItem? SideNavItem { get; set; }

    public bool HasAnyAccess => IsVisible && (CanRead || CanWrite || CanUpdate || CanDelete);
}
