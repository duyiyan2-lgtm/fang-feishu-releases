using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(AppDbContext db, PasswordHasher passwordHasher, IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword)
    {
        var query = db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Username.Contains(keyword) || x.RealName.Contains(keyword));
        }

        var users = await query.OrderBy(x => x.CreatedAt).ToListAsync();
        return OkData(users.Select(ToUserItem));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (await db.Users.AnyAsync(x => x.Username == request.Username))
        {
            return Fail(1101, "Username already exists.");
        }

        var roles = await ResolveRolesAsync(request.RoleCodes);
        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password),
            RealName = request.RealName,
            Email = request.Email,
            Phone = request.Phone,
            DepartmentId = request.DepartmentId,
            Profile = new EmployeeProfile { Position = request.Position }
        };
        user.UserRoles.AddRange(roles.Select(role => new UserRole { User = user, Role = role }));

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "User", "Create", user.Id.ToString(), HttpContext);

        var created = await LoadUserAsync(user.Id);
        return CreatedData(ToUserItem(created!));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
    {
        var user = await db.Users
            .Include(x => x.Profile)
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return Fail(1102, "User not found.", StatusCodes.Status404NotFound);
        }

        user.RealName = request.RealName ?? user.RealName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.DepartmentId = request.DepartmentId;
        user.Status = request.Status ?? user.Status;

        user.Profile ??= new EmployeeProfile { UserId = user.Id };
        user.Profile.Position = request.Position;

        if (request.RoleCodes is not null)
        {
            var roles = await ResolveRolesAsync(request.RoleCodes);
            user.UserRoles.Clear();
            user.UserRoles.AddRange(roles.Select(role => new UserRole { UserId = user.Id, RoleId = role.Id }));
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "User", "Update", user.Id.ToString(), HttpContext);

        var updated = await LoadUserAsync(user.Id);
        return OkData(ToUserItem(updated!));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetStatusRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return Fail(1102, "User not found.", StatusCodes.Status404NotFound);
        }

        user.Status = request.Status;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "User", "SetStatus", user.Id.ToString(), HttpContext);
        return OkData(new { user.Id, user.Status });
    }

    private async Task<List<Role>> ResolveRolesAsync(IReadOnlyList<string>? roleCodes)
    {
        var codes = roleCodes is { Count: > 0 } ? roleCodes : new[] { AppRoles.User };
        return await db.Roles.Where(x => codes.Contains(x.RoleCode)).ToListAsync();
    }

    private Task<User?> LoadUserAsync(Guid id)
    {
        return db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private static object ToUserItem(User user)
    {
        return new
        {
            user.Id,
            user.Username,
            user.RealName,
            user.Email,
            user.Phone,
            user.DepartmentId,
            DepartmentName = user.Department?.Name,
            user.Status,
            Position = user.Profile?.Position,
            Roles = user.UserRoles.Select(x => x.Role.RoleCode),
            user.CreatedAt
        };
    }
}

