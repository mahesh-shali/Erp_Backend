using Erp.Api.Caching;
using Erp.Api.Data;
using Erp.Api.Models;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(AppDbContext db, CacheService cache) : ControllerBase
{
    [Authorize(Policy = AppPermissions.DepartmentsView)]
    [HttpGet]
    public async Task<IReadOnlyList<Department>> GetAll()
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.Departments,
            async () => await db.Departments
                .AsNoTracking()
                .OrderBy(department => department.Code)
                .ToListAsync(),
            TimeSpan.FromMinutes(5));
    }

    [Authorize(Policy = AppPermissions.DepartmentsManage)]
    [HttpPost]
    public async Task<ActionResult<Department>> Create(DepartmentRequest request)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim()
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();
        await cache.RemoveAsync(CacheKeys.Departments);
        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, department);
    }
}

public sealed record DepartmentRequest(string Code, string Name);
