using Erp.Api.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Grpc;

public sealed class InternalErpService(
    AppDbContext db) : InternalErp.InternalErpBase
{
    public override async Task<SystemSummaryReply> GetSystemSummary(
        SystemSummaryRequest request,
        ServerCallContext context)
    {
        return new SystemSummaryReply
        {
            UserCount = await db.Users.CountAsync(context.CancellationToken),
            RoleCount = await db.Roles.CountAsync(context.CancellationToken),
            DepartmentCount = await db.Departments.CountAsync(context.CancellationToken)
        };
    }
}
