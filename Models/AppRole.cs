namespace Erp.Api.Models;

public sealed class AppRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedDate { get; set; }

    public ICollection<AppUser> Users { get; set; } = [];
    public ICollection<RolePermission> Permissions { get; set; } = [];
    public ICollection<RoleSideNavPermission> SideNavPermissions { get; set; } = [];
}
