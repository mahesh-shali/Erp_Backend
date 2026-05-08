using Microsoft.AspNetCore.Authorization;

namespace Erp.Api.Permissions;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
