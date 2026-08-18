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

public sealed class TasksControllerTests
{
    [Fact]
    public async Task CreateAndComplete_ShouldNotifyAssignee_AndEnforcePermissions()
    {
        await using var db = CreateDbContext();
        var creator = CreateUser("creator", "Creator");
        var assignee = CreateUser("assignee", "Assignee");
        var other = CreateUser("other", "Other");
        db.Users.AddRange(creator, assignee, other);
        await db.SaveChangesAsync();

        var creatorController = CreateController(db, creator.Id);
        var create = Assert.IsType<ObjectResult>(await creatorController.Create(new CreateTaskRequest(
            "Prepare release notes",
            "Summarize this iteration.",
            assignee.Id,
            DateTimeOffset.UtcNow.AddDays(1))));
        Assert.Equal(StatusCodes.Status201Created, create.StatusCode);

        var task = await db.WorkTasks.SingleAsync();
        Assert.Equal(creator.Id, task.CreatorId);
        Assert.Equal(assignee.Id, task.AssigneeId);
        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(assignee.Id, notification.UserId);
        Assert.Equal("Task", notification.ResourceType);
        Assert.Equal(task.Id, notification.ResourceId);

        var otherController = CreateController(db, other.Id);
        var forbidden = Assert.IsType<ObjectResult>(await otherController.Update(task.Id, new UpdateTaskRequest(
            "Changed title", null, null, null)));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);

        var assigneeController = CreateController(db, assignee.Id);
        var completed = Assert.IsType<OkObjectResult>(await assigneeController.Complete(task.Id));
        Assert.Equal(StatusCodes.Status200OK, completed.StatusCode);
        Assert.Equal("Completed", (await db.WorkTasks.SingleAsync()).Status);
        Assert.NotNull((await db.WorkTasks.SingleAsync()).CompletedAt);
    }

    private static User CreateUser(string username, string realName)
    {
        return new User
        {
            Username = username,
            RealName = realName,
            PasswordHash = "hash",
            Status = "Active"
        };
    }

    private static TasksController CreateController(AppDbContext db, Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.User)
        }, "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.TraceIdentifier = Guid.NewGuid().ToString("N");
        return new TasksController(db, new AuditService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
