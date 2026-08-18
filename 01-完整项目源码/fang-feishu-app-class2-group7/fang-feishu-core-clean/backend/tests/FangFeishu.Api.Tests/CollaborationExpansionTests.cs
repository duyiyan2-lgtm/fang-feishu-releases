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
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class CollaborationExpansionTests
{
    [Fact]
    public async Task CalendarInvitation_ShouldAllowAttendeeToRespond()
    {
        await using var db = CreateDbContext();
        var organizer = CreateUser("organizer", "Organizer");
        var attendee = CreateUser("attendee", "Attendee");
        db.Users.AddRange(organizer, attendee);
        await db.SaveChangesAsync();

        var organizerController = CreateCalendarController(db, organizer.Id);
        var created = Assert.IsType<ObjectResult>(await organizerController.Create(new CalendarEventRequest(
            "Project review",
            DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow.AddHours(3),
            "Online",
            null,
            new[] { attendee.Id },
            "Daily",
            DateTimeOffset.UtcNow.AddDays(2))));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var calendarEvent = await db.CalendarEvents.SingleAsync();
        Assert.Single(await db.CalendarEventAttendees.ToListAsync());
        Assert.Single(await db.Notifications.Where(x => x.UserId == attendee.Id).ToListAsync());

        var attendeeController = CreateCalendarController(db, attendee.Id);
        var response = Assert.IsType<OkObjectResult>(await attendeeController.UpdateAttendance(
            calendarEvent.Id,
            new CalendarAttendanceRequest("Accepted")));
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("Accepted", (await db.CalendarEventAttendees.SingleAsync()).Status);

        var occurrences = Assert.IsType<OkObjectResult>(await organizerController.Occurrences(
            calendarEvent.Id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(3)));
        var occurrenceData = occurrences.Value!.GetType().GetProperty("Data")!.GetValue(occurrences.Value);
        Assert.True(Assert.IsAssignableFrom<IEnumerable<object>>(occurrenceData).Count() >= 2);
    }

    [Fact]
    public async Task WikiMemberWithEditPermission_ShouldCreateNode()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("wiki_owner", "Wiki Owner");
        var editor = CreateUser("wiki_editor", "Wiki Editor");
        db.Users.AddRange(owner, editor);
        await db.SaveChangesAsync();

        var ownerController = CreateWikiController(db, owner.Id);
        var created = Assert.IsType<ObjectResult>(await ownerController.CreateSpace(new WikiSpaceRequest(
            "Engineering handbook", "Team knowledge", "Private")));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var space = await db.WikiSpaces.SingleAsync();

        var memberResult = Assert.IsType<OkObjectResult>(await ownerController.SetMembers(
            space.Id,
            new WikiMemberRequest(new[] { editor.Id }, "Edit")));
        Assert.Equal(StatusCodes.Status200OK, memberResult.StatusCode);

        var editorController = CreateWikiController(db, editor.Id);
        var nodeResult = Assert.IsType<ObjectResult>(await editorController.CreateNode(
            space.Id,
            new WikiNodeRequest(null, null, "Release process")));
        Assert.Equal(StatusCodes.Status201Created, nodeResult.StatusCode);
        Assert.Equal("Release process", (await db.WikiNodes.SingleAsync()).Title);
    }

    [Fact]
    public async Task TemplateApproval_ShouldAdvanceThroughConfiguredApprover()
    {
        await using var db = CreateDbContext();
        var administrator = CreateUser("administrator", "Administrator");
        var applicant = CreateUser("applicant", "Applicant");
        var approver = CreateUser("approver", "Approver");
        db.Users.AddRange(administrator, applicant, approver);
        await db.SaveChangesAsync();

        var adminController = CreateApprovalsController(db, administrator.Id, true);
        var templateResult = Assert.IsType<ObjectResult>(await adminController.CreateTemplate(new ApprovalTemplateRequest(
            "Leave template", "Leave", "One-step leave", new[] { approver.Id })));
        Assert.Equal(StatusCodes.Status201Created, templateResult.StatusCode);
        var template = await db.ApprovalTemplates.SingleAsync();

        var applicantController = CreateApprovalsController(db, applicant.Id);
        var submitted = Assert.IsType<ObjectResult>(await applicantController.Submit(new ApprovalRequest(
            "Leave", "One day leave", "Personal affairs", template.Id, new[] { administrator.Id })));
        Assert.Equal(StatusCodes.Status201Created, submitted.StatusCode);
        var approval = await db.ApprovalInstances.SingleAsync();
        Assert.Equal("Pending", approval.Status);
        Assert.Equal(2, await db.Notifications.CountAsync());

        var approverController = CreateApprovalsController(db, approver.Id);
        var approved = Assert.IsType<OkObjectResult>(await approverController.Approve(
            approval.Id,
            new ApprovalActionRequest("Approved")));
        Assert.Equal(StatusCodes.Status200OK, approved.StatusCode);
        Assert.Equal("Approved", (await db.ApprovalInstances.SingleAsync()).Status);
    }

    [Fact]
    public async Task FileSharing_ShouldExposeSharedFileToRecipient()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("file_owner", "File Owner");
        var recipient = CreateUser("file_recipient", "File Recipient");
        var file = new StoredFile
        {
            FileName = "plan.md",
            FilePath = "test/plan.md",
            FileSize = 100,
            ContentType = "text/markdown",
            Uploader = owner
        };
        db.Users.AddRange(owner, recipient);
        db.Files.Add(file);
        await db.SaveChangesAsync();

        var ownerController = CreateFilesController(db, owner.Id);
        var shared = Assert.IsType<OkObjectResult>(await ownerController.SetShares(
            file.Id,
            new FileShareRequest(new[] { recipient.Id }, "View")));
        Assert.Equal(StatusCodes.Status200OK, shared.StatusCode);

        var recipientController = CreateFilesController(db, recipient.Id);
        var listed = Assert.IsType<OkObjectResult>(await recipientController.List(null, null));
        var data = listed.Value!.GetType().GetProperty("Data")!.GetValue(listed.Value);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(data));
    }

    [Fact]
    public async Task MeetingScheduleAndChat_ShouldWorkForMeetingMember()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("meeting_owner", "Meeting Owner");
        var member = CreateUser("meeting_member", "Meeting Member");
        var meeting = new Meeting
        {
            Title = "Weekly sync",
            RoomId = "weekly_sync",
            ChannelName = "weekly_sync",
            Creator = owner,
            Members = new List<MeetingMember>
            {
                new() { User = owner, Role = "Owner", JoinedAt = DateTime.UtcNow },
                new() { User = member }
            }
        };
        db.Users.AddRange(owner, member);
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();

        var ownerController = CreateMeetingsController(db, owner.Id);
        var scheduled = Assert.IsType<OkObjectResult>(await ownerController.UpdateSchedule(
            meeting.Id,
            new UpdateMeetingScheduleRequest(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2))));
        Assert.Equal(StatusCodes.Status200OK, scheduled.StatusCode);

        var memberController = CreateMeetingsController(db, member.Id);
        var chat = Assert.IsType<ObjectResult>(await memberController.SendChatMessage(
            meeting.Id,
            new SendMeetingChatMessageRequest("I am online.")));
        Assert.Equal(StatusCodes.Status201Created, chat.StatusCode);
        Assert.Equal("I am online.", (await db.MeetingChatMessages.SingleAsync()).Content);
    }

    private static CalendarController CreateCalendarController(AppDbContext db, Guid userId)
    {
        return new CalendarController(db, new AuditService(db))
        {
            ControllerContext = CreateContext(userId)
        };
    }

    private static WikiController CreateWikiController(AppDbContext db, Guid userId)
    {
        return new WikiController(db, new AuditService(db))
        {
            ControllerContext = CreateContext(userId)
        };
    }

    private static ApprovalsController CreateApprovalsController(AppDbContext db, Guid userId, bool isAdmin = false)
    {
        return new ApprovalsController(db, new AuditService(db))
        {
            ControllerContext = CreateContext(userId, isAdmin)
        };
    }

    private static FilesController CreateFilesController(AppDbContext db, Guid userId)
    {
        return new FilesController(db, new NoopFileStorageService(), new AuditService(db))
        {
            ControllerContext = CreateContext(userId)
        };
    }

    private static MeetingsController CreateMeetingsController(AppDbContext db, Guid userId)
    {
        return new MeetingsController(db, new AgoraTokenService(Options.Create(new AgoraOptions())), new AuditService(db))
        {
            ControllerContext = CreateContext(userId)
        };
    }

    private static ControllerContext CreateContext(Guid userId, bool isAdmin = false)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.Add(new Claim(ClaimTypes.Role, isAdmin ? AppRoles.Admin : AppRoles.User));
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        return new ControllerContext { HttpContext = context };
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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoopFileStorageService : IFileStorageService
    {
        public Task<StorageSaveResult> SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageSaveResult("test/file", request.ContentLength, request.ContentType));
        }

        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
