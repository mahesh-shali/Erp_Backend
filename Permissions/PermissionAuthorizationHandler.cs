using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Erp.Api.Permissions;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission) ||
            context.User.HasClaim(ClaimTypes.Role, "Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
