namespace Erp.Api.Models;

public sealed class SideNavItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public int Level { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedDate { get; set; }

    public SideNavItem? Parent { get; set; }
    public ICollection<SideNavItem> Children { get; set; } = [];
    public ICollection<RoleSideNavPermission> RolePermissions { get; set; } = [];
}
