using System.Security.Claims;
using System.Text.Json;
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

public sealed class ImControllerTests
{
    [Fact]
    public async Task Conversations_ShouldReturnOwnerAdminsAndSettings()
    {
        await using var db = CreateDbContext();
        var (owner, admin, _, conversation) = await SeedGroupConversationAsync(db);
        conversation.AdminIdsJson = JsonSerializer.Serialize(new[] { admin.Id });
        conversation.InvitePermission = "admin";
        conversation.KickPermission = "admin";
        conversation.EditNamePermission = "all";
        await db.SaveChangesAsync();

        var controller = CreateController(db, owner.Id);
        var result = Assert.IsType<OkObjectResult>(await controller.Conversations());
        var data = GetData(result);
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(data));

        Assert.Equal(owner.Id, GetProperty<Guid>(item, "OwnerId"));
        Assert.Contains(admin.Id, Assert.IsAssignableFrom<IEnumerable<Guid>>(GetProperty<object>(item, "AdminIds")));

        var settings = GetProperty<object>(item, "Settings");
        Assert.Equal("admin", GetProperty<string>(settings, "InvitePermission"));
        Assert.Equal("admin", GetProperty<string>(settings, "KickPermission"));
        Assert.Equal("all", GetProperty<string>(settings, "EditNamePermission"));
    }

    [Fact]
    public async Task SetAdmins_ShouldRequireOwnerAndPersistFullList()
    {
        await using var db = CreateDbContext();
        var (owner, admin, member, conversation) = await SeedGroupConversationAsync(db);

        var ownerController = CreateController(db, owner.Id);
        var success = Assert.IsType<OkObjectResult>(await ownerController.SetAdmins(
            conversation.Id,
            new SetConversationAdminsRequest(new[] { admin.Id, member.Id })));
        Assert.Equal(StatusCodes.Status200OK, success.StatusCode);

        var saved = await db.Conversations.SingleAsync(x => x.Id == conversation.Id);
        var adminIds = JsonSerializer.Deserialize<List<Guid>>(saved.AdminIdsJson!);
        Assert.NotNull(adminIds);
        Assert.Contains(admin.Id, adminIds!);
        Assert.Contains(member.Id, adminIds!);

        var memberController = CreateController(db, member.Id);
        var forbidden = Assert.IsType<ObjectResult>(await memberController.SetAdmins(
            conversation.Id,
            new SetConversationAdminsRequest(Array.Empty<Guid>())));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task DeleteConversation_ShouldRequireOwnerAndHideDissolvedConversation()
    {
        await using var db = CreateDbContext();
        var (owner, _, member, conversation) = await SeedGroupConversationAsync(db);

        var memberController = CreateController(db, member.Id);
        var forbidden = Assert.IsType<ObjectResult>(await memberController.DeleteConversation(conversation.Id));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);

        var ownerController = CreateController(db, owner.Id);
        var deleted = Assert.IsType<OkObjectResult>(await ownerController.DeleteConversation(conversation.Id));
        Assert.Equal(StatusCodes.Status200OK, deleted.StatusCode);
        Assert.Equal("dissolved", (await db.Conversations.SingleAsync(x => x.Id == conversation.Id)).Status);

        var listAfterDissolve = Assert.IsType<OkObjectResult>(await memberController.Conversations());
        var data = GetData(listAfterDissolve);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<object>>(data));
    }

    [Fact]
    public async Task GroupMembers_ShouldSupportInviteAndOwnerRemoval()
    {
        await using var db = CreateDbContext();
        var (owner, _, _, conversation) = await SeedGroupConversationAsync(db);
        var invited = CreateUser("invited", "Invited User");
        db.Users.Add(invited);
        await db.SaveChangesAsync();

        var ownerController = CreateController(db, owner.Id);
        var added = Assert.IsType<OkObjectResult>(await ownerController.AddMembers(
            conversation.Id,
            new UpdateConversationMembersRequest(new[] { invited.Id })));
        Assert.Equal(StatusCodes.Status200OK, added.StatusCode);
        Assert.True(await db.ConversationMembers.AnyAsync(x => x.ConversationId == conversation.Id && x.UserId == invited.Id));

        var removed = Assert.IsType<OkObjectResult>(await ownerController.RemoveMember(conversation.Id, invited.Id));
        Assert.Equal(StatusCodes.Status200OK, removed.StatusCode);
        Assert.False(await db.ConversationMembers.AnyAsync(x => x.ConversationId == conversation.Id && x.UserId == invited.Id));
    }

    [Fact]
    public async Task MessageReaction_ShouldBeAddedByConversationMember()
    {
        await using var db = CreateDbContext();
        var (owner, _, member, conversation) = await SeedGroupConversationAsync(db);
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = owner.Id,
            Content = "Please review this update."
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var memberController = CreateController(db, member.Id);
        var result = Assert.IsType<OkObjectResult>(await memberController.AddReaction(
            message.Id,
            new MessageReactionRequest("thumbsup")));
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        var reaction = await db.MessageReactions.SingleAsync();
        Assert.Equal(member.Id, reaction.UserId);
        Assert.Equal("thumbsup", reaction.ReactionType);
    }

    [Fact]
    public async Task GroupOwner_ShouldUpdateAnnouncement()
    {
        await using var db = CreateDbContext();
        var (owner, _, _, conversation) = await SeedGroupConversationAsync(db);
        var ownerController = CreateController(db, owner.Id);

        var result = Assert.IsType<OkObjectResult>(await ownerController.UpdateAnnouncement(
            conversation.Id,
            new UpdateConversationAnnouncementRequest("This week: finish integration testing.")));

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        var saved = await db.Conversations.SingleAsync(x => x.Id == conversation.Id);
        Assert.Equal("This week: finish integration testing.", saved.Announcement);
        Assert.NotNull(saved.AnnouncementUpdatedAt);
    }

    [Fact]
    public async Task CreateConversation_ShouldAcceptPrivateAliasAndReuseExistingConversation()
    {
        await using var db = CreateDbContext();
        var currentUser = CreateUser("current_user", "Current User");
        var contact = CreateUser("contact_user", "Contact User");
        db.Users.AddRange(currentUser, contact);
        await db.SaveChangesAsync();

        var controller = CreateController(db, currentUser.Id);
        var request = new CreateConversationRequest("Private", null, new[] { contact.Id });
        var created = Assert.IsType<ObjectResult>(await controller.CreateConversation(request));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var conversation = await db.Conversations.Include(x => x.Members).SingleAsync();
        Assert.Equal("Single", conversation.Type);
        Assert.Equal(2, conversation.Members.Count);

        var existing = Assert.IsType<OkObjectResult>(await controller.CreateConversation(request));
        var existingData = GetData(existing)!;
        Assert.Equal(conversation.Id, GetProperty<Guid>(existingData, "Id"));
        Assert.Equal(1, await db.Conversations.CountAsync());
    }

    [Fact]
    public async Task CreateConversation_ShouldRejectSelfOnlyRequest()
    {
        await using var db = CreateDbContext();
        var currentUser = CreateUser("self_user", "Self User");
        db.Users.Add(currentUser);
        await db.SaveChangesAsync();

        var controller = CreateController(db, currentUser.Id);
        var result = Assert.IsType<ObjectResult>(await controller.CreateConversation(
            new CreateConversationRequest("Private", null, new[] { currentUser.Id })));

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(1501, GetProperty<int>(result.Value!, "Code"));
    }

    private static async Task<(User Owner, User Admin, User Member, Conversation Conversation)> SeedGroupConversationAsync(AppDbContext db)
    {
        var owner = CreateUser("owner", "Owner");
        var admin = CreateUser("admin_user", "Admin User");
        var member = CreateUser("member", "Member");

        var conversation = new Conversation
        {
            Type = "Group",
            Title = "Project Team",
            CreatedBy = owner.Id,
            Status = "active"
        };
        conversation.Members.AddRange(new[]
        {
            new ConversationMember { Conversation = conversation, User = owner },
            new ConversationMember { Conversation = conversation, User = admin },
            new ConversationMember { Conversation = conversation, User = member }
        });

        db.Users.AddRange(owner, admin, member);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        return (owner, admin, member, conversation);
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

    private static ImController CreateController(AppDbContext db, Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.User)
        }, "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.TraceIdentifier = Guid.NewGuid().ToString("N");

        return new ImController(db, null!, new AuditService(db))
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

    private static object? GetData(ObjectResult result)
    {
        return result.Value!.GetType().GetProperty("Data")!.GetValue(result.Value);
    }

    private static T GetProperty<T>(object target, string name)
    {
        return (T)target.GetType().GetProperty(name)!.GetValue(target)!;
    }
}
