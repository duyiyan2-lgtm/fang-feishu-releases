using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/meetings")]
[Authorize]
public sealed class MeetingsController(
    AppDbContext db,
    AgoraTokenService agoraTokenService,
    IAuditService auditService,
    IRealtimeEventPublisher? realtimePublisher = null) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var normalizedStatus = NormalizeMeetingStatus(status);
        if (!string.IsNullOrWhiteSpace(status)
            && normalizedStatus is null
            && !status.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return Fail(2108, "Meeting status must be Active, Ended, or All.");
        }

        var query = db.Meetings
            .Include(x => x.Creator)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Where(x => CurrentUserIsAdmin || x.CreatedBy == CurrentUserId || x.Members.Any(m => m.UserId == CurrentUserId));

        if (normalizedStatus is not null)
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var meetings = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return OkData(meetings.Select(ToMeetingItem));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMeetingRequest request)
    {
        if (request.ScheduledStartAt.HasValue && request.ScheduledEndAt.HasValue && request.ScheduledEndAt <= request.ScheduledStartAt)
        {
            return Fail(2105, "Scheduled end time must be later than start time.");
        }

        var title = FirstNotBlank(request.Title, request.RoomName, "项目同步会议");
        var roomId = NormalizeRoomId(request.RoomId);

        while (await db.Meetings.AnyAsync(x => x.RoomId == roomId || x.ChannelName == roomId))
        {
            roomId = NormalizeRoomId(null);
        }

        var memberIds = (request.MemberUserIds ?? Array.Empty<Guid>())
            .Append(CurrentUserId)
            .Distinct()
            .ToList();

        var users = await db.Users.Where(x => memberIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        var meeting = new Meeting
        {
            Title = title,
            RoomId = roomId,
            ChannelName = roomId,
            CreatedBy = CurrentUserId,
            Status = "Active",
            ScheduledStartAt = request.ScheduledStartAt?.UtcDateTime,
            ScheduledEndAt = request.ScheduledEndAt?.UtcDateTime
        };

        foreach (var user in users)
        {
            meeting.Members.Add(new MeetingMember
            {
                UserId = user.Id,
                Role = user.Id == CurrentUserId ? "Owner" : "Member",
                JoinedAt = user.Id == CurrentUserId ? DateTime.UtcNow : null
            });

            if (user.Id != CurrentUserId)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Meeting invitation",
                    Content = title,
                    Type = "Meeting",
                    ResourceType = "Meeting",
                    ResourceId = meeting.Id
                });
            }
        }

        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Meeting", "Create", meeting.Id.ToString(), HttpContext);

        var created = await LoadMeetingAsync(meeting.Id);
        var payload = ToMeetingItem(created!);
        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUsersAsync(
                users.Where(x => x.Id != CurrentUserId).Select(x => x.Id),
                RealtimeEventNames.MeetingInvited,
                ToMeetingInvitedEvent(created!, CurrentUserId, created!.Creator.RealName));
        }

        return CreatedData(payload);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(meeting))
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(ToMeetingItem(meeting));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, MeetingJoinRequest _)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (meeting.Status == "Ended")
        {
            return Fail(2103, "Meeting has ended.");
        }

        var member = meeting.Members.FirstOrDefault(x => x.UserId == CurrentUserId);
        if (member is null)
        {
            if (meeting.CreatedBy != CurrentUserId && !CurrentUserIsAdmin)
            {
                return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
            }

            member = new MeetingMember { MeetingId = id, UserId = CurrentUserId, Role = "Member" };
            db.MeetingMembers.Add(member);
        }

        member.JoinedAt ??= DateTime.UtcNow;
        member.LeftAt = null;
        await db.SaveChangesAsync();

        var updated = await LoadMeetingAsync(id) ?? meeting;
        var agora = agoraTokenService.CreateJoinToken(CurrentUserId, meeting.ChannelName, CurrentClientType);
        if (!agora.Configured)
        {
            return Fail(2104, "Agora AppId is not configured. Set Agora:AppId in server configuration.");
        }

        await auditService.WriteAsync(CurrentUserId, "Meeting", "Join", meeting.Id.ToString(), HttpContext);

        return OkData(new
        {
            Provider = "Agora",
            Meeting = ToMeetingItem(updated),
            AppId = agora.AppId,
            ChannelName = meeting.ChannelName,
            RoomId = meeting.RoomId,
            Uid = agora.Uid,
            RtcToken = agora.RtcToken,
            TokenRequired = agora.TokenRequired,
            TokenExpireAt = agora.TokenExpireAt
        });
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        var member = meeting.Members.FirstOrDefault(x => x.UserId == CurrentUserId);
        if (member is null && meeting.CreatedBy != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        if (member is not null)
        {
            member.LeftAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        await auditService.WriteAsync(CurrentUserId, "Meeting", "Leave", meeting.Id.ToString(), HttpContext);
        var updated = await LoadMeetingAsync(id);
        return OkData(ToMeetingItem(updated!));
    }

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, InviteMeetingMembersRequest request)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (meeting.CreatedBy != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        if (meeting.Status == "Ended")
        {
            return Fail(2103, "Meeting has ended.");
        }

        var memberIds = (request.MemberUserIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var users = await db.Users.Where(x => memberIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        var existing = meeting.Members.Select(x => x.UserId).ToHashSet();
        var newlyInvitedUsers = users.Where(x => !existing.Contains(x.Id)).ToList();

        foreach (var user in newlyInvitedUsers)
        {
            db.MeetingMembers.Add(new MeetingMember { MeetingId = id, UserId = user.Id, Role = "Member" });
            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Meeting invitation",
                Content = meeting.Title,
                Type = "Meeting",
                ResourceType = "Meeting",
                ResourceId = meeting.Id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Meeting", "Invite", meeting.Id.ToString(), HttpContext);

        var updated = await LoadMeetingAsync(id);
        var payload = ToMeetingItem(updated!);
        if (realtimePublisher is not null)
        {
            var inviterName = await db.Users
                .Where(x => x.Id == CurrentUserId)
                .Select(x => x.RealName)
                .FirstOrDefaultAsync() ?? updated!.Creator.RealName;
            await realtimePublisher.SendToUsersAsync(
                newlyInvitedUsers.Select(x => x.Id),
                RealtimeEventNames.MeetingInvited,
                ToMeetingInvitedEvent(updated!, CurrentUserId, inviterName));
        }

        return OkData(payload);
    }

    [HttpPatch("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (meeting.CreatedBy != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        if (meeting.Status == "Ended")
        {
            return OkData(ToMeetingItem(meeting));
        }

        meeting.Status = "Ended";
        meeting.EndedAt = DateTime.UtcNow;
        foreach (var member in meeting.Members.Where(x => x.LeftAt is null))
        {
            member.LeftAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Meeting", "End", meeting.Id.ToString(), HttpContext);
        var payload = ToMeetingItem(meeting);
        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUsersAsync(
                meeting.Members.Select(x => x.UserId),
                RealtimeEventNames.MeetingEnded,
                ToMeetingEndedEvent(meeting));
        }

        return OkData(payload);
    }

    [HttpPatch("{id:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid id, UpdateMeetingScheduleRequest request)
    {
        if (request.ScheduledStartAt.HasValue && request.ScheduledEndAt.HasValue && request.ScheduledEndAt <= request.ScheduledStartAt)
        {
            return Fail(2105, "Scheduled end time must be later than start time.");
        }

        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (meeting.CreatedBy != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        meeting.ScheduledStartAt = request.ScheduledStartAt?.UtcDateTime;
        meeting.ScheduledEndAt = request.ScheduledEndAt?.UtcDateTime;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Meeting", "UpdateSchedule", meeting.Id.ToString(), HttpContext);
        return OkData(ToMeetingItem(meeting));
    }

    [HttpGet("{id:guid}/chat")]
    public async Task<IActionResult> ChatMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(meeting))
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.MeetingChatMessages
            .Include(x => x.Sender)
            .Where(x => x.MeetingId == id);
        var total = await query.CountAsync();
        var messages = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return OkData(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = messages.OrderBy(x => x.CreatedAt).Select(ToChatMessageItem)
        });
    }

    [HttpPost("{id:guid}/chat")]
    public async Task<IActionResult> SendChatMessage(Guid id, SendMeetingChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Fail(2106, "Meeting chat content is required.");
        }

        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(meeting))
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        if (meeting.Status == "Ended")
        {
            return Fail(2107, "Cannot send chat message to an ended meeting.");
        }

        var message = new MeetingChatMessage
        {
            MeetingId = id,
            SenderId = CurrentUserId,
            Content = request.Content.Trim()
        };
        db.MeetingChatMessages.Add(message);
        await db.SaveChangesAsync();
        await db.Entry(message).Reference(x => x.Sender).LoadAsync();
        await auditService.WriteAsync(CurrentUserId, "Meeting", "SendChatMessage", meeting.Id.ToString(), HttpContext);
        return CreatedData(ToChatMessageItem(message));
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<IActionResult> Statistics(Guid id)
    {
        var meeting = await LoadMeetingAsync(id);
        if (meeting is null)
        {
            return Fail(2101, "Meeting not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(meeting))
        {
            return Fail(2102, "No meeting permission.", StatusCodes.Status403Forbidden);
        }

        var now = DateTime.UtcNow;
        var joinedMembers = meeting.Members.Where(x => x.JoinedAt.HasValue).ToList();
        var totalSeconds = joinedMembers.Sum(member => ((member.LeftAt ?? now) - member.JoinedAt!.Value).TotalSeconds);
        return OkData(new
        {
            meeting.Id,
            InvitedCount = meeting.Members.Count,
            JoinedCount = joinedMembers.Count,
            OnlineCount = meeting.Members.Count(x => x.JoinedAt.HasValue && x.LeftAt is null),
            AverageParticipationSeconds = joinedMembers.Count == 0 ? 0 : Math.Round(totalSeconds / joinedMembers.Count, 0),
            meeting.Status,
            meeting.CreatedAt,
            meeting.EndedAt
        });
    }

    private async Task<Meeting?> LoadMeetingAsync(Guid id)
    {
        return await db.Meetings
            .Include(x => x.Creator)
            .Include(x => x.Members).ThenInclude(x => x.User).ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private bool CanAccess(Meeting meeting)
    {
        return CurrentUserIsAdmin || meeting.CreatedBy == CurrentUserId || meeting.Members.Any(x => x.UserId == CurrentUserId);
    }

    private static string FirstNotBlank(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "会议";
    }

    private static string NormalizeRoomId(string? roomId)
    {
        var value = string.IsNullOrWhiteSpace(roomId)
            ? CreateRoomId()
            : roomId.Trim();

        var chars = value.Select(ch => IsRoomIdChar(ch) ? ch : '_').ToArray();
        var normalized = new string(chars).Trim('_').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? CreateRoomId() : normalized;
    }

    private static string CreateRoomId()
    {
        return $"ff_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..25];
    }

    private static string? NormalizeMeetingStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => "Active",
            "ENDED" => "Ended",
            _ => null
        };
    }

    private static bool IsRoomIdChar(char ch)
    {
        return ch is >= 'a' and <= 'z'
            || ch is >= 'A' and <= 'Z'
            || ch is >= '0' and <= '9'
            || ch == '_'
            || ch == '-';
    }

    private object ToMeetingItem(Meeting meeting)
    {
        return new
        {
            meeting.Id,
            meeting.Provider,
            meeting.RoomId,
            meeting.ChannelName,
            meeting.Title,
            meeting.Status,
            meeting.CreatedBy,
            CreatorName = meeting.Creator.RealName,
            meeting.CreatedAt,
            meeting.EndedAt,
            meeting.ScheduledStartAt,
            meeting.ScheduledEndAt,
            // UserName and Username differ only by the capital N. With the Web JSON
            // contract (camelCase + case-insensitive metadata), anonymous-object
            // serialization treats them as a property-name collision and returns an
            // empty HTTP 500 response. A dictionary preserves the established wire
            // keys "userName" and "username" without CLR metadata collisions.
            Members = meeting.Members.OrderBy(x => x.InvitedAt).Select(x =>
                new Dictionary<string, object?>
                {
                    ["userId"] = x.UserId,
                    ["userName"] = x.User.RealName,
                    ["username"] = x.User.Username,
                    ["avatarUrl"] = x.User.Profile?.AvatarUrl,
                    ["role"] = x.Role,
                    ["invitedAt"] = x.InvitedAt,
                    ["joinedAt"] = x.JoinedAt,
                    ["leftAt"] = x.LeftAt,
                    ["rtcIdentities"] = new[]
                    {
                        new { ClientType = "Legacy", Uid = agoraTokenService.GetUid(x.UserId) },
                        new { ClientType = "Android", Uid = agoraTokenService.GetUid(x.UserId, "Android") },
                        new { ClientType = "Desktop", Uid = agoraTokenService.GetUid(x.UserId, "Desktop") },
                        new { ClientType = "Web", Uid = agoraTokenService.GetUid(x.UserId, "Web") },
                        new { ClientType = "MiniProgram", Uid = agoraTokenService.GetUid(x.UserId, "MiniProgram") }
                    }
                })
        };
    }

    private object ToMeetingInvitedEvent(Meeting meeting, Guid inviterId, string inviterName)
    {
        return new
        {
            MeetingId = meeting.Id,
            InviterId = inviterId,
            InviterName = inviterName,
            meeting.Title,
            meeting.RoomId,
            meeting.ChannelName,
            meeting.Status,
            Meeting = ToMeetingItem(meeting)
        };
    }

    private object ToMeetingEndedEvent(Meeting meeting)
    {
        return new
        {
            MeetingId = meeting.Id,
            meeting.Title,
            meeting.Status,
            meeting.EndedAt,
            Meeting = ToMeetingItem(meeting)
        };
    }

    private static object ToChatMessageItem(MeetingChatMessage message)
    {
        return new
        {
            message.Id,
            message.MeetingId,
            message.SenderId,
            SenderName = message.Sender.RealName,
            message.Content,
            message.CreatedAt
        };
    }
}
