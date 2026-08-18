using System.Security.Claims;
using FangFeishu.Api.Common;
using FangFeishu.Api.Controllers;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Tests;

public sealed class DocumentsControllerTests
{
    [Fact]
    public async Task Comments_ShouldListAndAllowOwnerToDelete()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("owner", "Owner");
        var author = CreateUser("author", "Author");
        var other = CreateUser("other", "Other");
        var document = new Document
        {
            Title = "Shared document",
            Content = "content",
            Owner = owner,
            UpdatedBy = owner.Id
        };
        var comment = new DocumentComment
        {
            Document = document,
            User = author,
            Content = "Please review this section."
        };

        db.Users.AddRange(owner, author, other);
        db.Documents.Add(document);
        db.DocumentComments.Add(comment);
        await db.SaveChangesAsync();

        var authorController = CreateController(db, author.Id);
        var listed = Assert.IsType<OkObjectResult>(await authorController.Comments(document.Id));
        var data = listed.Value!.GetType().GetProperty("Data")!.GetValue(listed.Value);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(data));

        var otherController = CreateController(db, other.Id);
        var forbidden = Assert.IsType<ObjectResult>(await otherController.DeleteComment(document.Id, comment.Id));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);

        var ownerController = CreateController(db, owner.Id);
        var deleted = Assert.IsType<OkObjectResult>(await ownerController.DeleteComment(document.Id, comment.Id));
        Assert.Equal(StatusCodes.Status200OK, deleted.StatusCode);
        Assert.False(await db.DocumentComments.AnyAsync());
    }

    [Fact]
    public async Task Delete_ShouldMoveDocumentToTrashAndSupportRestoreAndPermanentDelete()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("trash_owner", "Trash Owner");
        var document = new Document
        {
            Title = "Recoverable document",
            Content = "content",
            Owner = owner,
            UpdatedBy = owner.Id
        };
        db.Users.Add(owner);
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var controller = CreateController(db, owner.Id);
        var deleted = Assert.IsType<OkObjectResult>(await controller.Delete(document.Id));
        Assert.Equal(StatusCodes.Status200OK, deleted.StatusCode);
        Assert.False(await db.Documents.AnyAsync(x => x.Id == document.Id));
        Assert.True(await db.Documents.IgnoreQueryFilters().AnyAsync(x => x.Id == document.Id && x.IsDeleted));

        var trash = Assert.IsType<OkObjectResult>(await controller.Trash(null));
        var trashData = trash.Value!.GetType().GetProperty("Data")!.GetValue(trash.Value);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(trashData));

        var restored = Assert.IsType<OkObjectResult>(await controller.Restore(document.Id));
        Assert.Equal(StatusCodes.Status200OK, restored.StatusCode);
        Assert.True(await db.Documents.AnyAsync(x => x.Id == document.Id));

        await controller.Delete(document.Id);
        var permanent = Assert.IsType<OkObjectResult>(await controller.PermanentDelete(document.Id));
        Assert.Equal(StatusCodes.Status200OK, permanent.StatusCode);
        Assert.False(await db.Documents.IgnoreQueryFilters().AnyAsync(x => x.Id == document.Id));
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

    private static DocumentsController CreateController(AppDbContext db, Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.User)
        }, "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.TraceIdentifier = Guid.NewGuid().ToString("N");
        return new DocumentsController(db, new AuditService(db))
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
