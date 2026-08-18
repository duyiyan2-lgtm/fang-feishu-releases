using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/departments")]
[Authorize]
public sealed class DepartmentsController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet("tree")]
    public async Task<IActionResult> Tree()
    {
        var departments = await db.Departments.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync();
        var nodes = departments.ToDictionary(x => x.Id, x => new DepartmentNode(x.Id, x.ParentId, x.Name, x.SortOrder));

        foreach (var node in nodes.Values)
        {
            if (node.ParentId.HasValue && nodes.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        return OkData(nodes.Values.Where(x => x.ParentId is null).OrderBy(x => x.SortOrder));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(DepartmentRequest request)
    {
        var department = new Department { ParentId = request.ParentId, Name = request.Name, SortOrder = request.SortOrder };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Department", "Create", department.Id.ToString(), HttpContext);
        return CreatedData(new { department.Id, department.ParentId, department.Name, department.SortOrder });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(Guid id, DepartmentRequest request)
    {
        var department = await db.Departments.FindAsync(id);
        if (department is null)
        {
            return Fail(1301, "Department not found.", StatusCodes.Status404NotFound);
        }

        department.ParentId = request.ParentId;
        department.Name = request.Name;
        department.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Department", "Update", department.Id.ToString(), HttpContext);
        return OkData(new { department.Id, department.ParentId, department.Name, department.SortOrder });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var department = await db.Departments.FindAsync(id);
        if (department is null)
        {
            return Fail(1301, "Department not found.", StatusCodes.Status404NotFound);
        }

        db.Departments.Remove(department);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Department", "Delete", department.Id.ToString(), HttpContext);
        return OkData(new { id });
    }

    private sealed class DepartmentNode(Guid id, Guid? parentId, string name, int sortOrder)
    {
        public Guid Id { get; } = id;
        public Guid? ParentId { get; } = parentId;
        public string Name { get; } = name;
        public int SortOrder { get; } = sortOrder;
        public List<DepartmentNode> Children { get; } = new();
    }
}

