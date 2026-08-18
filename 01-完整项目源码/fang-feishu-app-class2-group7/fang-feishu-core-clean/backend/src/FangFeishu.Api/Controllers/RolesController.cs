using System.Text.Json;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/roles")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class RolesController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var roles = await db.Roles.OrderBy(x => x.CreatedAt).ToListAsync();
        return OkData(roles.Select(ToRoleItem));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        return role is null
            ? Fail(1202, "Role not found.", StatusCodes.Status404NotFound)
            : OkData(ToRoleItem(role));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoleRequest request)
    {
        if (!TryNormalizeRole(request, out var roleName, out var roleCode, out var permissions, out var error))
        {
            return Fail(1203, error);
        }

        if (await db.Roles.AnyAsync(x => x.RoleCode == roleCode))
        {
            return Fail(1201, "Role code already exists.", StatusCodes.Status409Conflict);
        }

        var role = new Role
        {
            RoleName = roleName,
            RoleCode = roleCode,
            Description = NormalizeOptionalText(request.Description),
            PermissionsJson = SerializePermissions(permissions)
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Role", "Create", role.Id.ToString(), HttpContext);
        return CreatedData(ToRoleItem(role));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RoleRequest request)
    {
        var role = await db.Roles.FindAsync(id);
        if (role is null)
        {
            return Fail(1202, "Role not found.", StatusCodes.Status404NotFound);
        }

        if (!TryNormalizeRole(request, out var roleName, out var roleCode, out var permissions, out var error))
        {
            return Fail(1203, error);
        }

        if (await db.Roles.AnyAsync(x => x.Id != id && x.RoleCode == roleCode))
        {
            return Fail(1201, "Role code already exists.", StatusCodes.Status409Conflict);
        }

        role.RoleName = roleName;
        role.RoleCode = roleCode;
        role.Description = NormalizeOptionalText(request.Description);
        role.PermissionsJson = SerializePermissions(permissions);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Role", "Update", role.Id.ToString(), HttpContext);
        return OkData(ToRoleItem(role));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request)
    {
        var role = await db.Roles.FindAsync(id);
        if (role is null)
        {
            return Fail(1202, "Role not found.", StatusCodes.Status404NotFound);
        }

        if (!TryNormalizePermissions(request.Permissions, out var permissions, out var error))
        {
            return Fail(1204, error);
        }

        role.PermissionsJson = SerializePermissions(permissions);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Role", "UpdatePermissions", role.Id.ToString(), HttpContext);
        return OkData(ToRoleItem(role));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await db.Roles.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (role is null)
        {
            return Fail(1202, "Role not found.", StatusCodes.Status404NotFound);
        }

        if (role.UserRoles.Count > 0)
        {
            return Fail(1205, "Role is assigned to users and cannot be deleted.", StatusCodes.Status409Conflict);
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Role", "Delete", role.Id.ToString(), HttpContext);
        return OkData(new { role.Id });
    }

    private static object ToRoleItem(Role role) => new
    {
        role.Id,
        role.RoleName,
        role.RoleCode,
        role.Description,
        Permissions = DeserializePermissions(role.PermissionsJson),
        role.CreatedAt
    };

    private static bool TryNormalizeRole(
        RoleRequest request,
        out string roleName,
        out string roleCode,
        out IReadOnlyList<string> permissions,
        out string error)
    {
        roleName = request.RoleName?.Trim() ?? string.Empty;
        roleCode = request.RoleCode?.Trim() ?? string.Empty;
        permissions = Array.Empty<string>();
        error = string.Empty;

        if (roleName.Length is < 1 or > 64)
        {
            error = "Role name must be 1-64 characters.";
            return false;
        }

        if (roleCode.Length is < 1 or > 64 || !roleCode.All(IsRoleCodeCharacter))
        {
            error = "Role code must be 1-64 characters and contain only letters, digits, underscores, or hyphens.";
            return false;
        }

        return TryNormalizePermissions(request.Permissions, out permissions, out error);
    }

    private static bool TryNormalizePermissions(
        IReadOnlyList<string>? requested,
        out IReadOnlyList<string> permissions,
        out string error)
    {
        var normalized = (requested ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count > 500 || normalized.Any(x => x.Length > 128 || !x.All(IsPermissionCharacter)))
        {
            permissions = Array.Empty<string>();
            error = "Permissions must contain at most 500 valid permission codes.";
            return false;
        }

        permissions = normalized;
        error = string.Empty;
        return true;
    }

    private static bool IsRoleCodeCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '_' or '-';

    private static bool IsPermissionCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '_' or '-' or '.' or ':' or '*';

    private static string SerializePermissions(IReadOnlyList<string> permissions) =>
        JsonSerializer.Serialize(permissions);

    private static IReadOnlyList<string> DeserializePermissions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
