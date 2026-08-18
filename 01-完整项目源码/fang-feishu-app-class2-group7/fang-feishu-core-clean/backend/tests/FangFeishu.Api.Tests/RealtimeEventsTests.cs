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
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class RealtimeEventsTests
{
    [Fact]
    public async Task AddedNotification_ShouldPublishReceiveNotificationAfterSave()
    {
        var publisher = new RecordingRealtimeEventPublisher();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new NotificationRealtimeInterceptor(publisher))
            .Options;
        await using var db = new AppDbContext(options);
        var user = CreateUser("notification_user", "Notification User");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "A new task",
            Content = "Please review it.",
            Type = "Task",
            ResourceType = "Task",
            ResourceId = Guid.NewGuid()
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var realtimeEvent = Assert.Single(publisher.Events);
        Assert.Equal(user.Id, realtimeEvent.UserId);
        Assert.Equal(RealtimeEventNames.ReceiveNotification, realtimeEvent.EventName);
        Assert.Equal(notification.Id, GetProperty<Guid>(realtimeEvent.Payload, "Id"));
    }

    [Fact]
    public async Task FriendRequestLifecycle_ShouldPublishReceivedAndAcceptedEvents()
    {
        await using var db = CreateDbContext();
        var requester = CreateUser("friend_requester", "Friend Requester");
        var recipient = CreateUser("friend_recipient", "Friend Recipient");
        db.Users.AddRange(requester, recipient);
        await db.SaveChangesAsync();
        var publisher = new RecordingRealtimeEventPublisher();

        var requesterController = new ContactsController(db, publisher)
        {
            ControllerContext = CreateContext(requester.Id)
        };
        await requesterController.SendRequest(new CreateFriendRequest(recipient.Id, "Hello"));

        var received = Assert.Single(
            publisher.Events,
            x => x.EventName == RealtimeEventNames.FriendRequestReceived);
        Assert.Equal(recipient.Id, received.UserId);

        var friendship = await db.Friendships.SingleAsync();
        var recipientController = new ContactsController(db, publisher)
        {
            ControllerContext = CreateContext(recipient.Id)
        };
        await recipientController.Accept(friendship.Id);

        var accepted = publisher.Events
            .Where(x => x.EventName == RealtimeEventNames.FriendRequestAccepted)
            .ToList();
        Assert.Equal(2, accepted.Count);
        Assert.Contains(accepted, x => x.UserId == requester.Id);
        Assert.Contains(accepted, x => x.UserId == recipient.Id);
    }

    [Fact]
    public async Task MeetingLifecycle_ShouldPublishInviteAndEndedEvents()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("meeting_owner_rt", "Meeting Owner");
        var member = CreateUser("meeting_member_rt", "Meeting Member");
        member.Profile = new EmployeeProfile
        {
            UserId = member.Id,
            AvatarUrl = "https://example.com/avatars/meeting-member.png"
        };
        db.Users.AddRange(owner, member);
        await db.SaveChangesAsync();
        var publisher = new RecordingRealtimeEventPublisher();
        var controller = new MeetingsController(
            db,
            new AgoraTokenService(Options.Create(new AgoraOptions())),
            new AuditService(db),
            publisher)
        {
            ControllerContext = CreateContext(owner.Id)
        };

        var created = Assert.IsType<ObjectResult>(await controller.Create(new CreateMeetingRequest(
            "Realtime meeting",
            null,
            null,
            new[] { member.Id })));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var responseData = GetProperty<object>(created.Value!, "Data");
        var responseMembers = Assert.IsAssignableFrom<IEnumerable<object>>(
            GetProperty<object>(responseData, "Members"));
        var responseMember = Assert.Single(
            responseMembers,
            item => GetProperty<Guid>(item, "UserId") == member.Id);
        Assert.Equal(
            member.Profile.AvatarUrl,
            GetProperty<string?>(responseMember, "AvatarUrl"));
        var rtcIdentities = Assert.IsAssignableFrom<IEnumerable<object>>(
            GetProperty<object>(responseMember, "RtcIdentities")).ToList();
        Assert.Contains(rtcIdentities, identity =>
            GetProperty<string>(identity, "ClientType") == "Legacy"
            && GetProperty<uint>(identity, "Uid") > 0);
        Assert.Contains(rtcIdentities, identity =>
            GetProperty<string>(identity, "ClientType") == "Android"
            && GetProperty<uint>(identity, "Uid") > 0);

        var webJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        var createJson = JsonSerializer.Serialize(created.Value, webJsonOptions);
        using (var createDocument = JsonDocument.Parse(createJson))
        {
            var serializedMember = createDocument.RootElement
                .GetProperty("data")
                .GetProperty("members")
                .EnumerateArray()
                .Single(item => item.GetProperty("username").GetString() == member.Username);
            Assert.Equal(member.RealName, serializedMember.GetProperty("userName").GetString());
            Assert.Equal(member.Username, serializedMember.GetProperty("username").GetString());
            Assert.Equal(member.Profile.AvatarUrl, serializedMember.GetProperty("avatarUrl").GetString());
        }

        var invite = Assert.Single(
            publisher.Events,
            x => x.EventName == RealtimeEventNames.MeetingInvited);
        Assert.Equal(member.Id, invite.UserId);

        var meeting = await db.Meetings.SingleAsync();
        Assert.Equal("MeetingInvited", invite.EventName);
        Assert.Equal(meeting.Id, GetProperty<Guid>(invite.Payload, "MeetingId"));
        Assert.Equal(owner.Id, GetProperty<Guid>(invite.Payload, "InviterId"));
        Assert.Equal(owner.RealName, GetProperty<string>(invite.Payload, "InviterName"));
        Assert.Equal(meeting.Title, GetProperty<string>(invite.Payload, "Title"));
        Assert.NotNull(GetProperty<object>(invite.Payload, "Meeting"));

        var list = Assert.IsType<OkObjectResult>(await controller.List(null));
        var listJson = JsonSerializer.Serialize(list.Value, webJsonOptions);
        using (var listDocument = JsonDocument.Parse(listJson))
        {
            var serializedMember = listDocument.RootElement
                .GetProperty("data")[0]
                .GetProperty("members")
                .EnumerateArray()
                .Single(item => item.GetProperty("username").GetString() == member.Username);
            Assert.Equal(member.RealName, serializedMember.GetProperty("userName").GetString());
            Assert.Equal(member.Username, serializedMember.GetProperty("username").GetString());
        }

        await controller.End(meeting.Id);

        var ended = publisher.Events
            .Where(x => x.EventName == RealtimeEventNames.MeetingEnded)
            .ToList();
        Assert.Equal(2, ended.Count);
        Assert.Contains(ended, x => x.UserId == owner.Id);
        Assert.Contains(ended, x => x.UserId == member.Id);
        Assert.All(ended, x =>
        {
            Assert.Equal(meeting.Id, GetProperty<Guid>(x.Payload, "MeetingId"));
            Assert.Equal("Ended", GetProperty<string>(x.Payload, "Status"));
            Assert.NotNull(GetProperty<object>(x.Payload, "Meeting"));
        });
    }

    [Fact]
    public async Task MeetingInvite_ShouldRequireOwnerAndRejectEndedMeeting()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("meeting_owner_permission", "Meeting Owner");
        var member = CreateUser("meeting_member_permission", "Meeting Member");
        var candidate = CreateUser("meeting_candidate", "Meeting Candidate");
        db.Users.AddRange(owner, member, candidate);
        await db.SaveChangesAsync();
        var publisher = new RecordingRealtimeEventPublisher();
        var ownerController = new MeetingsController(
            db,
            new AgoraTokenService(Options.Create(new AgoraOptions())),
            new AuditService(db),
            publisher)
        {
            ControllerContext = CreateContext(owner.Id)
        };

        await ownerController.Create(new CreateMeetingRequest(
            "Permission meeting",
            null,
            null,
            new[] { member.Id }));
        var meeting = await db.Meetings.SingleAsync();
        publisher.Events.Clear();

        var memberController = new MeetingsController(
            db,
            new AgoraTokenService(Options.Create(new AgoraOptions())),
            new AuditService(db),
            publisher)
        {
            ControllerContext = CreateContext(member.Id)
        };
        var denied = Assert.IsType<ObjectResult>(await memberController.Invite(
            meeting.Id,
            new InviteMeetingMembersRequest(new[] { candidate.Id })));
        Assert.Equal(StatusCodes.Status403Forbidden, denied.StatusCode);
        Assert.Equal(2102, GetProperty<int>(denied.Value!, "Code"));

        await ownerController.End(meeting.Id);
        publisher.Events.Clear();
        var ended = Assert.IsType<ObjectResult>(await ownerController.Invite(
            meeting.Id,
            new InviteMeetingMembersRequest(new[] { candidate.Id })));
        Assert.Equal(StatusCodes.Status400BadRequest, ended.StatusCode);
        Assert.Equal(2103, GetProperty<int>(ended.Value!, "Code"));
        Assert.Empty(publisher.Events);
        Assert.False(await db.MeetingMembers.AnyAsync(x =>
            x.MeetingId == meeting.Id && x.UserId == candidate.Id));
    }

    [Fact]
    public async Task MeetingList_ShouldFilterStatusCaseInsensitively()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("meeting_list_owner", "Meeting List Owner");
        db.Users.Add(owner);
        await db.SaveChangesAsync();
        var controller = new MeetingsController(
            db,
            new AgoraTokenService(Options.Create(new AgoraOptions())),
            new AuditService(db))
        {
            ControllerContext = CreateContext(owner.Id)
        };

        await controller.Create(new CreateMeetingRequest("Ended meeting", null, null, Array.Empty<Guid>()));
        var endedMeeting = await db.Meetings.SingleAsync();
        await controller.End(endedMeeting.Id);
        await controller.Create(new CreateMeetingRequest("Active meeting", null, null, Array.Empty<Guid>()));

        var endedResponse = Assert.IsType<OkObjectResult>(await controller.List("ended"));
        var endedItems = Assert.IsAssignableFrom<IEnumerable<object>>(
            GetProperty<object>(endedResponse.Value!, "Data")).ToList();
        var endedItem = Assert.Single(endedItems);
        Assert.Equal("Ended", GetProperty<string>(endedItem, "Status"));

        var allResponse = Assert.IsType<OkObjectResult>(await controller.List("all"));
        var allItems = Assert.IsAssignableFrom<IEnumerable<object>>(
            GetProperty<object>(allResponse.Value!, "Data")).ToList();
        Assert.Equal(2, allItems.Count);

        var invalid = Assert.IsType<ObjectResult>(await controller.List("unknown"));
        Assert.Equal(StatusCodes.Status400BadRequest, invalid.StatusCode);
        Assert.Equal(2108, GetProperty<int>(invalid.Value!, "Code"));
    }

    [Fact]
    public async Task CamelCaseConversationContract_ShouldBindTitleAndCreateGroup()
    {
        await using var db = CreateDbContext();
        var owner = CreateUser("group_owner", "Group Owner");
        var member = CreateUser("group_member", "Group Member");
        db.Users.AddRange(owner, member);
        await db.SaveChangesAsync();

        var json = $$"""
            {
              "type": "Group",
              "title": "Project Group",
              "memberUserIds": ["{{member.Id}}"]
            }
            """;
        var request = JsonSerializer.Deserialize<CreateConversationRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);

        var controller = new ImController(db, null!, new AuditService(db))
        {
            ControllerContext = CreateContext(owner.Id)
        };
        var result = Assert.IsType<ObjectResult>(await controller.CreateConversation(request!));

        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        var conversation = await db.Conversations.SingleAsync();
        Assert.Equal("Project Group", conversation.Title);
        Assert.Equal(2, await db.ConversationMembers.CountAsync());
    }

    [Fact]
    public void CamelCaseRegisterContract_ShouldBindPascalCaseRecord()
    {
        const string json = """
            {
              "username": "new_user",
              "password": "secret123",
              "realName": "New User",
              "email": "new.user@example.com",
              "phone": null,
              "clientType": "Web"
            }
            """;

        var request = JsonSerializer.Deserialize<RegisterRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("new_user", request!.Username);
        Assert.Equal("New User", request.RealName);
        Assert.Equal("Web", request.ClientType);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static ControllerContext CreateContext(Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AppRoles.User)
        }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.TraceIdentifier = Guid.NewGuid().ToString("N");
        return new ControllerContext { HttpContext = httpContext };
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

    private static T GetProperty<T>(object target, string propertyName)
    {
        if (target is IReadOnlyDictionary<string, object?> dictionary)
        {
            var entry = dictionary.First(pair =>
                string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase));
            return (T)entry.Value!;
        }

        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }

    private sealed record PublishedEvent(Guid UserId, string EventName, object Payload);

    private sealed class RecordingRealtimeEventPublisher : IRealtimeEventPublisher
    {
        public List<PublishedEvent> Events { get; } = new();

        public Task SendToUserAsync(
            Guid userId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new PublishedEvent(userId, eventName, payload));
            return Task.CompletedTask;
        }

        public async Task SendToUsersAsync(
            IEnumerable<Guid> userIds,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default)
        {
            foreach (var userId in userIds.Distinct())
            {
                await SendToUserAsync(userId, eventName, payload, cancellationToken);
            }
        }
    }
}
