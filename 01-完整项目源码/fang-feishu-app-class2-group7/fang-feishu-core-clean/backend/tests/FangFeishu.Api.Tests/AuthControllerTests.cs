using FangFeishu.Api.Controllers;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ShouldAcceptTwoCharacterChineseUsername()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new Role { RoleName = "User", RoleCode = AppRoles.User });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Register(new RegisterRequest(
            "火山",
            "secret123",
            "火山",
            null,
            null));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("火山", (await db.Users.SingleAsync()).Username);
    }

    [Fact]
    public async Task Register_ShouldCreateNormalUser_ReturnToken_AndRejectDuplicateUsername()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new Role { RoleName = "User", RoleCode = AppRoles.User });
        await db.SaveChangesAsync();

        var passwordHasher = new PasswordHasher();
        var controller = CreateController(db, passwordHasher);

        var result = await controller.Register(new RegisterRequest(
            "new_user",
            "secret123",
            "New User",
            "new.user@example.com",
            "13800000001"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var user = await db.Users.Include(x => x.UserRoles).SingleAsync();
        Assert.Equal("new_user", user.Username);
        Assert.True(passwordHasher.Verify("secret123", user.PasswordHash));
        Assert.Single(user.UserRoles);
        Assert.Equal(1, await db.OperationLogs.CountAsync());

        var duplicate = await controller.Register(new RegisterRequest(
            "new_user",
            "another-secret",
            "Duplicate User",
            null,
            null));

        var conflict = Assert.IsType<ObjectResult>(duplicate);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    private static AuthController CreateController(AppDbContext db, PasswordHasher? passwordHasher = null)
    {
        return new AuthController(
            db,
            passwordHasher ?? new PasswordHasher(),
            new JwtTokenService(Options.Create(new JwtOptions { Secret = "test-secret-at-least-thirty-two-chars" })),
            new AuditService(db),
            new TokenRevocationService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
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
