using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Controllers;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class SocialAndProfileTests
{
    [Fact]
    public async Task UpdateProfile_ShouldPersistAvatarAndProfileFields()
    {
        await using var db = CreateDbContext();
        var user = CreateUser("profile_user", "Before");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, user.Id);
        var result = Assert.IsType<OkObjectResult>(await controller.UpdateMe(new UpdateCurrentUserProfileRequest(
            "After",
            "after@example.com",
            "13800000001",
            "Mobile Developer",
            "https://alxy.fun/api/v1/files/avatar/preview",
            "Demo Office",
            "Updated from Android")));

        var response = Assert.IsType<ApiResponse<CurrentUserResponse>>(result.Value);
        Assert.Equal("After", response.Data!.RealName);
        Assert.Equal("Mobile Developer", response.Data.Position);
        Assert.Equal("https://alxy.fun/api/v1/files/avatar/preview", response.Data.AvatarUrl);

        var stored = await db.Users.Include(x => x.Profile).SingleAsync();
        Assert.Equal("After", stored.RealName);
        Assert.Equal("Demo Office", stored.Profile!.WorkPlace);
        Assert.Equal("Updated from Android", stored.Profile.Bio);
    }

    [Fact]
    public async Task FriendRequest_ShouldRequireAcceptanceBeforeContactAppears()
    {
        await using var db = CreateDbContext();
        var requester = CreateUser("requester", "Requester");
        var recipient = CreateUser("recipient", "Recipient");
        db.Users.AddRange(requester, recipient);
        await db.SaveChangesAsync();

        var requesterController = new ContactsController(db) { ControllerContext = CreateContext(requester.Id) };
        var recipientController = new ContactsController(db) { ControllerContext = CreateContext(recipient.Id) };

        await requesterController.SendRequest(new CreateFriendRequest(recipient.Id, "Let us collaborate."));
        Assert.Equal(0, await ContactCount(requesterController));
        Assert.Single(await db.Friendships.Where(x => x.Status == "Pending").ToListAsync());

        var pending = await db.Friendships.SingleAsync();
        await recipientController.Accept(pending.Id);

        Assert.Equal(1, await ContactCount(requesterController));
        Assert.Equal(1, await ContactCount(recipientController));
        Assert.Equal("Accepted", (await db.Friendships.SingleAsync()).Status);
    }

    [Fact]
    public async Task SameClientLogin_ShouldInvalidateOldDevice_WithoutAffectingOtherClientTypes()
    {
        await using var db = CreateDbContext();
        var role = new Role { RoleName = "User", RoleCode = AppRoles.User };
        var user = CreateUser("multi_device", "Multi Device");
        user.UserRoles.Add(new UserRole { User = user, Role = role });
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, user.Id);
        var firstAndroid = LoginData(await controller.Login(new LoginRequest("multi_device", "123456", "Android")));
        var secondAndroid = LoginData(await controller.Login(new LoginRequest("multi_device", "123456", "Android")));
        var web = LoginData(await controller.Login(new LoginRequest("multi_device", "123456", "Web")));

        var handler = new JwtSecurityTokenHandler();
        var firstAndroidToken = handler.ReadJwtToken(firstAndroid.Token);
        var secondAndroidToken = handler.ReadJwtToken(secondAndroid.Token);
        var webToken = handler.ReadJwtToken(web.Token);
        var androidSession = await db.UserClientSessions.SingleAsync(x => x.UserId == user.Id && x.ClientType == "Android");
        var webSession = await db.UserClientSessions.SingleAsync(x => x.UserId == user.Id && x.ClientType == "Web");

        Assert.NotEqual(firstAndroid.Token, secondAndroid.Token);
        Assert.NotEqual(
            firstAndroidToken.Claims.Single(x => x.Type == JwtTokenService.ClientSessionVersionClaim).Value,
            androidSession.SessionVersion.ToString());
        Assert.Equal(
            secondAndroidToken.Claims.Single(x => x.Type == JwtTokenService.ClientSessionVersionClaim).Value,
            androidSession.SessionVersion.ToString());
        Assert.Equal(
            webToken.Claims.Single(x => x.Type == JwtTokenService.ClientSessionVersionClaim).Value,
            webSession.SessionVersion.ToString());
    }

    private static LoginResponse LoginData(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<ApiResponse<LoginResponse>>(ok.Value).Data!;
    }

    private static async Task<int> ContactCount(ContactsController controller)
    {
        var result = Assert.IsType<OkObjectResult>(await controller.List());
        var data = result.Value!.GetType().GetProperty("Data")!.GetValue(result.Value);
        return Assert.IsAssignableFrom<IEnumerable<object>>(data).Count();
    }

    private static AuthController CreateAuthController(AppDbContext db, Guid userId)
    {
        return new AuthController(
            db,
            new PasswordHasher(),
            new JwtTokenService(Options.Create(new JwtOptions { Secret = "test-secret-at-least-thirty-two-chars" })),
            new AuditService(db),
            new TokenRevocationService(db))
        {
            ControllerContext = CreateContext(userId)
        };
    }

    private static ControllerContext CreateContext(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.User)
        };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        return new ControllerContext { HttpContext = context };
    }

    private static User CreateUser(string username, string realName)
    {
        var hasher = new PasswordHasher();
        return new User
        {
            Username = username,
            RealName = realName,
            PasswordHash = hasher.Hash("123456"),
            Status = "Active",
            Profile = new EmployeeProfile()
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
