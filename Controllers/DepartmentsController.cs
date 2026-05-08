using Erp.Api.Data;
using Erp.Api.Models;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(AppDbContext db) : ControllerBase
{
    [Authorize(Policy = AppPermissions.DepartmentsView)]
    [HttpGet]
    public async Task<IReadOnlyList<Department>> GetAll()
    {
        return await db.Departments.OrderBy(department => department.Code).ToListAsync();
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
        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, department);
    }
}

public sealed record DepartmentRequest(string Code, string Name);
