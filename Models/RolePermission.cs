namespace Erp.Api.Models;

public sealed class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;

    public AppRole? Role { get; set; }
}
