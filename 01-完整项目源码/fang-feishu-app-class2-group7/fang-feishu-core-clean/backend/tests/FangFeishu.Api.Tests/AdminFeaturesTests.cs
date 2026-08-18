using System.Security.Claims;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Controllers;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Tests;

public sealed class AdminFeaturesTests
{
    [Fact]
    public async Task RolePermissions_ShouldPersistThroughCreateAndDedicatedUpdateEndpoint()
    {
        await using var db = CreateDbContext();
        var admin = CreateAdmin();
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var controller = CreateController(new RolesController(db, new AuditService(db)), admin.Id);

        var created = Assert.IsType<ObjectResult>(await controller.Create(new RoleRequest(
            "Project manager",
            "project_manager",
            "Manages projects",
            new[] { "project.read", "project.write" })));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var role = await db.Roles.SingleAsync();
        Assert.Contains("project.write", role.PermissionsJson!);

        var updated = Assert.IsType<OkObjectResult>(await controller.UpdatePermissions(
            role.Id,
            new UpdateRolePermissionsRequest(new[] { "project.read", "meeting.manage" })));
        var data = GetData(updated);
        var permissions = Assert.IsAssignableFrom<IEnumerable<string>>(GetProperty<object>(data, "Permissions"));
        Assert.Contains("meeting.manage", permissions);
        Assert.DoesNotContain("project.write", permissions);
    }

    [Fact]
    public async Task Dictionary_ShouldPersistCategoryAndItems()
    {
        await using var db = CreateDbContext();
        var admin = CreateAdmin();
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var controller = CreateController(new DictionaryController(db, new AuditService(db)), admin.Id);

        var categoryResult = Assert.IsType<ObjectResult>(await controller.CreateCategory(
            new DictionaryCategoryRequest("approval_status", "审批状态", null)));
        Assert.Equal(StatusCodes.Status201Created, categoryResult.StatusCode);

        var itemResult = Assert.IsType<ObjectResult>(await controller.CreateItem(
            "approval_status",
            new DictionaryItemRequest("approved", "已通过", "Approved", null, 1)));
        Assert.Equal(StatusCodes.Status201Created, itemResult.StatusCode);

        var list = Assert.IsType<OkObjectResult>(await controller.Items("approval_status"));
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(GetData(list));
        var item = Assert.Single(items);
        Assert.Equal("approved", GetProperty<string>(item, "Code"));
        Assert.Equal("已通过", GetProperty<string>(item, "Label"));
    }

    private static T CreateController<T>(T controller, Guid userId) where T : ControllerBase
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.Admin)
        }, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateAdmin() => new()
    {
        Username = "feature_admin",
        RealName = "Feature Admin",
        PasswordHash = "hash",
        Status = "Active"
    };

    private static object GetData(ObjectResult result) =>
        result.Value!.GetType().GetProperty("Data")!.GetValue(result.Value)!;

    private static T GetProperty<T>(object target, string name) =>
        (T)target.GetType().GetProperty(name)!.GetValue(target)!;
}
