using System.Text.Json;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Hubs;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/im")]
[Authorize]
public sealed class ImController(AppDbContext db, IHubContext<ImHub> hubContext, IAuditService auditService) : BaseApiController
{
    private const string StatusActive = "active";
    private const string StatusDissolved = "dissolved";
    private const string PermissionAll = "all";
    private const string PermissionAdmin = "admin";

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations()
    {
        var conversations = await db.Conversations
            .Include(x => x.Members).ThenInclude(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.Messages).ThenInclude(x => x.Sender)
            .Include(x => x.Messages).ThenInclude(x => x.File)
            .Include(x => x.Messages).ThenInclude(x => x.Reactions).ThenInclude(x => x.User)
            .Where(x => x.Status != StatusDissolved && x.Members.Any(m => m.UserId == CurrentUserId))
            .OrderByDescending(x => x.Messages.Max(m => (DateTime?)m.CreatedAt) ?? x.CreatedAt)
            .ToListAsync();

        return OkData(conversations.Select(x => ToConversationItem(x, CurrentUserId)));
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(CreateConversationRequest request)
    {
        var otherMemberIds = (request.MemberUserIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty && x != CurrentUserId)
            .Distinct()
            .ToList();
        if (otherMemberIds.Count == 0)
        {
            return Fail(1501, "A conversation requires at least one other member.");
        }

        var type = NormalizeConversationType(request.Type);
        if (type is null)
        {
            return Fail(1529, "Conversation type must be Private, Single, or Group.");
        }
        if (type == "Single" && otherMemberIds.Count != 1)
        {
            return Fail(1530, "A private conversation must contain exactly one other member.");
        }

        var memberIds = otherMemberIds.Append(CurrentUserId).ToList();
        var users = await db.Users
            .Include(x => x.Profile)
            .Where(x => memberIds.Contains(x.Id) && x.Status == "Active")
            .ToListAsync();
        if (users.Count != memberIds.Count)
        {
            return Fail(1502, "Some members do not exist or are disabled.");
        }

        if (type == "Single")
        {
            var otherUserId = otherMemberIds[0];
            var existing = await db.Conversations
                .Include(x => x.Members).ThenInclude(x => x.User).ThenInclude(x => x.Profile)
                .Include(x => x.Messages).ThenInclude(x => x.Sender)
                .Include(x => x.Messages).ThenInclude(x => x.File)
                .Include(x => x.Messages).ThenInclude(x => x.Reactions).ThenInclude(x => x.User)
                .Where(x => x.Status != StatusDissolved &&
                    (x.Type == "Single" || x.Type == "Private") &&
                    x.Members.Count == 2 &&
                    x.Members.Any(m => m.UserId == CurrentUserId) &&
                    x.Members.Any(m => m.UserId == otherUserId))
                .FirstOrDefaultAsync();
            if (existing is not null)
            {
                return OkData(ToConversationItem(existing, CurrentUserId), "conversation already exists");
            }
        }

        var conversation = new Conversation
        {
            Type = type,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            CreatedBy = CurrentUserId,
            Status = StatusActive
        };
        SetAdminIds(conversation, Array.Empty<Guid>());
        conversation.Members.AddRange(users.Select(user => new ConversationMember { Conversation = conversation, User = user }));
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "CreateConversation", conversation.Id.ToString(), HttpContext);

        return CreatedData(ToConversationItem(conversation, CurrentUserId));
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> ConversationDetail(Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsCurrentUserMember(conversation))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(ToConversationItem(conversation, CurrentUserId));
    }

    [HttpPut("conversations/{conversationId:guid}")]
    public async Task<IActionResult> UpdateConversation(Guid conversationId, UpdateConversationRequest request)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsCurrentUserMember(conversation))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can be managed.");
        }

        var adminIds = GetAdminIds(conversation);
        var isOwner = conversation.CreatedBy == CurrentUserId;
        var isAdmin = adminIds.Contains(CurrentUserId);

        if (request.Settings is not null)
        {
            if (!isOwner)
            {
                return Fail(1509, "Only group owner can update permissions.", StatusCodes.Status403Forbidden);
            }

            if (!TryNormalizePermission(request.Settings.InvitePermission, conversation.InvitePermission, out var invitePermission) ||
                !TryNormalizePermission(request.Settings.KickPermission, conversation.KickPermission, out var kickPermission) ||
                !TryNormalizePermission(request.Settings.EditNamePermission, conversation.EditNamePermission, out var editNamePermission))
            {
                return Fail(1510, "Permission value must be all or admin.");
            }

            conversation.InvitePermission = invitePermission;
            conversation.KickPermission = kickPermission;
            conversation.EditNamePermission = editNamePermission;
        }

        if (request.Status is not null)
        {
            var status = request.Status.Trim().ToLowerInvariant();
            if (status is not StatusActive and not StatusDissolved)
            {
                return Fail(1511, "Status value must be active or dissolved.");
            }

            if (!isOwner)
            {
                return Fail(1512, "Only group owner can update group status.", StatusCodes.Status403Forbidden);
            }

            conversation.Status = status;
        }

        if (request.Title is not null)
        {
            if (!CanEditName(conversation, CurrentUserId, adminIds))
            {
                return Fail(1513, "No permission to update group name.", StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Fail(1514, "Group title is required.");
            }

            conversation.Title = request.Title.Trim();
        }

        if (request.Avatar is not null)
        {
            if (!isOwner && !isAdmin)
            {
                return Fail(1515, "Only group owner or admin can update avatar.", StatusCodes.Status403Forbidden);
            }

            conversation.Avatar = string.IsNullOrWhiteSpace(request.Avatar) ? null : request.Avatar.Trim();
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "UpdateConversation", conversation.Id.ToString(), HttpContext);

        if (IsDissolved(conversation))
        {
            await PublishConversationDissolvedAsync(conversation);
        }
        else
        {
            await PublishConversationUpdatedAsync(conversation);
        }

        return OkData(ToConversationItem(conversation, CurrentUserId));
    }

    [HttpGet("conversations/{conversationId:guid}/announcement")]
    public async Task<IActionResult> Announcement(Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsCurrentUserMember(conversation))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(new { conversation.Id, conversation.Announcement, conversation.AnnouncementUpdatedAt });
    }

    [HttpPut("conversations/{conversationId:guid}/announcement")]
    public async Task<IActionResult> UpdateAnnouncement(Guid conversationId, UpdateConversationAnnouncementRequest request)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can have announcements.");
        }

        var canUpdate = conversation.CreatedBy == CurrentUserId || GetAdminIds(conversation).Contains(CurrentUserId);
        if (!canUpdate)
        {
            return Fail(1526, "Only group owner or admin can update announcement.", StatusCodes.Status403Forbidden);
        }

        var announcement = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim();
        if (announcement?.Length > 2000)
        {
            return Fail(1527, "Announcement must be 2000 characters or fewer.");
        }

        conversation.Announcement = announcement;
        conversation.AnnouncementUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "UpdateAnnouncement", conversation.Id.ToString(), HttpContext);
        await PublishConversationUpdatedAsync(conversation);
        if (hubContext is not null)
        {
            await hubContext.Clients.Group($"conversation:{conversation.Id}")
                .SendAsync("ConversationAnnouncementUpdated", new
                {
                    conversation.Id,
                    conversation.Announcement,
                    conversation.AnnouncementUpdatedAt
                });
        }

        return OkData(new { conversation.Id, conversation.Announcement, conversation.AnnouncementUpdatedAt });
    }

    [HttpPut("conversations/{conversationId:guid}/admins")]
    public async Task<IActionResult> SetAdmins(Guid conversationId, SetConversationAdminsRequest request)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can be managed.");
        }

        if (conversation.CreatedBy != CurrentUserId)
        {
            return Fail(1509, "Only group owner can update admins.", StatusCodes.Status403Forbidden);
        }

        var memberIds = conversation.Members.Select(x => x.UserId).ToHashSet();
        var adminIds = (request.AdminIds ?? Array.Empty<Guid>())
            .Where(x => x != conversation.CreatedBy)
            .Distinct()
            .ToList();

        if (adminIds.Any(x => !memberIds.Contains(x)))
        {
            return Fail(1516, "Admins must be group members.");
        }

        SetAdminIds(conversation, adminIds);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "SetConversationAdmins", conversation.Id.ToString(), HttpContext);
        await PublishConversationUpdatedAsync(conversation);

        return OkData(ToConversationItem(conversation, CurrentUserId));
    }

    [HttpPost("conversations/{conversationId:guid}/members")]
    public async Task<IActionResult> AddMembers(Guid conversationId, UpdateConversationMembersRequest request)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can be managed.");
        }

        if (!CanInviteMembers(conversation, CurrentUserId, GetAdminIds(conversation)))
        {
            return Fail(1517, "No permission to invite group members.", StatusCodes.Status403Forbidden);
        }

        var existingMemberIds = conversation.Members.Select(x => x.UserId).ToHashSet();
        var newMemberIds = (request.MemberUserIds ?? Array.Empty<Guid>())
            .Where(x => !existingMemberIds.Contains(x))
            .Distinct()
            .ToList();
        if (newMemberIds.Count == 0)
        {
            return OkData(ToConversationItem(conversation, CurrentUserId));
        }

        var users = await db.Users
            .Include(x => x.Profile)
            .Where(x => newMemberIds.Contains(x.Id) && x.Status == "Active")
            .ToListAsync();
        if (users.Count != newMemberIds.Count)
        {
            return Fail(1518, "Some invited users do not exist or are disabled.");
        }

        foreach (var user in users)
        {
            db.ConversationMembers.Add(new ConversationMember { ConversationId = conversation.Id, UserId = user.Id });
            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Added to group conversation",
                Content = conversation.Title ?? "Group conversation",
                Type = "IM",
                ResourceType = "Conversation",
                ResourceId = conversation.Id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "AddConversationMembers", conversation.Id.ToString(), HttpContext);

        var updated = (await LoadConversationAsync(conversation.Id))!;
        await PublishConversationUpdatedAsync(updated);
        return OkData(ToConversationItem(updated, CurrentUserId));
    }

    [HttpDelete("conversations/{conversationId:guid}/members/{memberUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid conversationId, Guid memberUserId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can be managed.");
        }

        var adminIds = GetAdminIds(conversation);
        if (!CanRemoveMembers(conversation, CurrentUserId, adminIds))
        {
            return Fail(1519, "No permission to remove group members.", StatusCodes.Status403Forbidden);
        }

        if (memberUserId == conversation.CreatedBy)
        {
            return Fail(1520, "Group owner cannot be removed. Dissolve or transfer ownership first.");
        }

        var member = conversation.Members.FirstOrDefault(x => x.UserId == memberUserId);
        if (member is null)
        {
            return Fail(1521, "Group member not found.", StatusCodes.Status404NotFound);
        }

        if (adminIds.Contains(memberUserId) && conversation.CreatedBy != CurrentUserId)
        {
            return Fail(1519, "Only group owner can remove an administrator.", StatusCodes.Status403Forbidden);
        }

        db.ConversationMembers.Remove(member);
        if (adminIds.Contains(memberUserId))
        {
            SetAdminIds(conversation, adminIds.Where(x => x != memberUserId));
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "RemoveConversationMember", conversation.Id.ToString(), HttpContext);

        await PublishConversationRemovedAsync(conversation.Id, memberUserId);
        var updated = (await LoadConversationAsync(conversation.Id))!;
        await PublishConversationUpdatedAsync(updated);
        return OkData(ToConversationItem(updated, CurrentUserId));
    }

    [HttpPost("conversations/{conversationId:guid}/leave")]
    public async Task<IActionResult> LeaveConversation(Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsGroupConversation(conversation))
        {
            return Fail(1508, "Only group conversations can be managed.");
        }

        if (conversation.CreatedBy == CurrentUserId)
        {
            return Fail(1522, "Group owner cannot leave. Dissolve or transfer ownership first.");
        }

        var member = conversation.Members.FirstOrDefault(x => x.UserId == CurrentUserId);
        if (member is null)
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        db.ConversationMembers.Remove(member);
        var adminIds = GetAdminIds(conversation);
        if (adminIds.Contains(CurrentUserId))
        {
            SetAdminIds(conversation, adminIds.Where(x => x != CurrentUserId));
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "LeaveConversation", conversation.Id.ToString(), HttpContext);

        await PublishConversationRemovedAsync(conversation.Id, CurrentUserId);
        var updated = (await LoadConversationAsync(conversation.Id))!;
        await PublishConversationUpdatedAsync(updated);
        return OkData(new { conversation.Id, Left = true });
    }

    [HttpDelete("conversations/{conversationId:guid}")]
    public Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        return DissolveConversationAsync(conversationId);
    }

    [HttpPost("conversations/{conversationId:guid}/dissolve")]
    public Task<IActionResult> Dissolve(Guid conversationId)
    {
        return DissolveConversationAsync(conversationId);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> Messages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        if (!await IsActiveMemberAsync(conversationId))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Messages.CountAsync(x => x.ConversationId == conversationId);
        var messages = await db.Messages
            .Include(x => x.Sender)
            .Include(x => x.File)
            .Include(x => x.Reactions).ThenInclude(x => x.User)
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return OkData(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = messages.OrderBy(x => x.CreatedAt).Select(ToMessageItem)
        });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> Send(SendMessageRequest request)
    {
        if (!await IsActiveMemberAsync(request.ConversationId))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Fail(1504, "Message content is required.");
        }

        var members = await db.ConversationMembers.Where(x => x.ConversationId == request.ConversationId).ToListAsync();
        var memberIds = members.Select(x => x.UserId).ToHashSet();
        var mentionUserIds = (request.MentionUserIds ?? Array.Empty<Guid>())
            .Where(x => x != CurrentUserId)
            .Distinct()
            .ToList();
        if (mentionUserIds.Any(x => !memberIds.Contains(x)))
        {
            return Fail(1528, "Mentioned users must be conversation members.");
        }

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = CurrentUserId,
            Content = request.Content,
            MessageType = request.MessageType,
            FileId = request.FileId,
            MentionUserIdsJson = JsonSerializer.Serialize(mentionUserIds)
        };

        db.Messages.Add(message);
        foreach (var member in members.Where(x => x.UserId != CurrentUserId))
        {
            db.Notifications.Add(new Notification
            {
                UserId = member.UserId,
                Title = mentionUserIds.Contains(member.UserId) ? "You were mentioned" : "New message",
                Content = request.Content.Length > 80 ? request.Content[..80] : request.Content,
                Type = "IM",
                ResourceType = "Conversation",
                ResourceId = request.ConversationId
            });
        }

        await db.SaveChangesAsync();
        await db.Entry(message).Reference(x => x.Sender).LoadAsync();
        if (message.FileId.HasValue)
        {
            await db.Entry(message).Reference(x => x.File).LoadAsync();
        }

        var payload = ToMessageItem(message);
        await hubContext.Clients.Group($"conversation:{request.ConversationId}").SendAsync("ReceiveMessage", payload);
        foreach (var member in members)
        {
            await hubContext.Clients.Group($"user:{member.UserId}").SendAsync("ReceiveMessage", payload);
        }

        return CreatedData(payload);
    }

    [HttpGet("conversations/{conversationId:guid}/read-receipts")]
    public async Task<IActionResult> ReadReceipts(Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (!IsCurrentUserMember(conversation))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(conversation.Members.Select(member => new
        {
            member.UserId,
            UserName = member.User.RealName,
            member.LastReadMessageId,
            LastReadAt = member.LastReadMessageId is null
                ? member.JoinedAt
                : conversation.Messages.FirstOrDefault(x => x.Id == member.LastReadMessageId)?.CreatedAt
        }));
    }

    [HttpGet("messages/search")]
    public async Task<IActionResult> SearchMessages([FromQuery] string keyword, [FromQuery] Guid? conversationId)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Fail(1523, "Search keyword is required.");
        }

        var query = db.Messages
            .Include(x => x.Conversation)
            .Include(x => x.Sender)
            .Include(x => x.File)
            .Include(x => x.Reactions).ThenInclude(x => x.User)
            .Where(x =>
                x.Conversation.Status != StatusDissolved &&
                x.Conversation.Members.Any(member => member.UserId == CurrentUserId) &&
                x.Content.Contains(keyword));
        if (conversationId.HasValue)
        {
            query = query.Where(x => x.ConversationId == conversationId.Value);
        }

        var messages = await query.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
        return OkData(messages.Select(x => new
        {
            Message = ToMessageItem(x),
            ConversationTitle = x.Conversation.Title
        }));
    }

    [HttpPost("messages/{messageId:guid}/reactions")]
    public async Task<IActionResult> AddReaction(Guid messageId, MessageReactionRequest request)
    {
        var message = await LoadMessageAsync(messageId);
        if (message is null)
        {
            return Fail(1505, "Message not found.", StatusCodes.Status404NotFound);
        }

        if (!await IsActiveMemberAsync(message.ConversationId))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        if (message.IsRecalled)
        {
            return Fail(1524, "Cannot react to a recalled message.");
        }

        var reactionType = NormalizeReactionType(request.ReactionType);
        if (reactionType is null)
        {
            return Fail(1525, "Reaction type is required and must be 32 characters or fewer.");
        }

        if (!message.Reactions.Any(x => x.UserId == CurrentUserId && x.ReactionType == reactionType))
        {
            db.MessageReactions.Add(new MessageReaction
            {
                MessageId = message.Id,
                UserId = CurrentUserId,
                ReactionType = reactionType
            });
            await db.SaveChangesAsync();
        }

        var updated = (await LoadMessageAsync(messageId))!;
        await PublishReactionUpdatedAsync(updated);
        return OkData(ToMessageItem(updated));
    }

    [HttpDelete("messages/{messageId:guid}/reactions/{reactionType}")]
    public async Task<IActionResult> RemoveReaction(Guid messageId, string reactionType)
    {
        var message = await LoadMessageAsync(messageId);
        if (message is null)
        {
            return Fail(1505, "Message not found.", StatusCodes.Status404NotFound);
        }

        if (!await IsActiveMemberAsync(message.ConversationId))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        var normalizedType = NormalizeReactionType(reactionType);
        if (normalizedType is null)
        {
            return Fail(1525, "Reaction type is required and must be 32 characters or fewer.");
        }

        var reaction = message.Reactions.FirstOrDefault(x => x.UserId == CurrentUserId && x.ReactionType == normalizedType);
        if (reaction is not null)
        {
            db.MessageReactions.Remove(reaction);
            await db.SaveChangesAsync();
        }

        var updated = (await LoadMessageAsync(messageId))!;
        await PublishReactionUpdatedAsync(updated);
        return OkData(ToMessageItem(updated));
    }

    [HttpPatch("messages/{messageId:guid}/recall")]
    public async Task<IActionResult> Recall(Guid messageId)
    {
        var message = await db.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
        if (message is null)
        {
            return Fail(1505, "Message not found.", StatusCodes.Status404NotFound);
        }

        if (message.SenderId != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(1506, "Only sender or admin can recall message.", StatusCodes.Status403Forbidden);
        }

        message.IsRecalled = true;
        await db.SaveChangesAsync();
        await hubContext.Clients.Group($"conversation:{message.ConversationId}").SendAsync("MessageRecalled", new { message.Id, message.ConversationId });
        return OkData(new { message.Id, message.IsRecalled });
    }

    [HttpPatch("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId)
    {
        var member = await db.ConversationMembers
            .Include(x => x.Conversation)
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == CurrentUserId);
        if (member is null || IsDissolved(member.Conversation))
        {
            return Fail(1503, "No conversation permission.", StatusCodes.Status403Forbidden);
        }

        var lastMessageId = await db.Messages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        member.LastReadMessageId = lastMessageId;
        await db.SaveChangesAsync();
        return OkData(new { conversationId, member.LastReadMessageId });
    }

    private async Task<IActionResult> DissolveConversationAsync(Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);
        if (conversation is null || IsDissolved(conversation))
        {
            return Fail(1507, "Conversation not found.", StatusCodes.Status404NotFound);
        }

        if (conversation.CreatedBy != CurrentUserId)
        {
            return Fail(1509, "Only group owner can dissolve conversation.", StatusCodes.Status403Forbidden);
        }

        conversation.Status = StatusDissolved;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "IM", "DissolveConversation", conversation.Id.ToString(), HttpContext);
        await PublishConversationDissolvedAsync(conversation);

        return OkData(new { conversation.Id, conversation.Status });
    }

    private async Task<Conversation?> LoadConversationAsync(Guid conversationId)
    {
        return await db.Conversations
            .Include(x => x.Members).ThenInclude(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.Messages).ThenInclude(x => x.Sender)
            .Include(x => x.Messages).ThenInclude(x => x.File)
            .Include(x => x.Messages).ThenInclude(x => x.Reactions).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == conversationId);
    }

    private Task<Message?> LoadMessageAsync(Guid messageId)
    {
        return db.Messages
            .Include(x => x.Sender)
            .Include(x => x.File)
            .Include(x => x.Reactions).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == messageId);
    }

    private Task<bool> IsActiveMemberAsync(Guid conversationId)
    {
        return db.Conversations.AnyAsync(x =>
            x.Id == conversationId &&
            x.Status != StatusDissolved &&
            x.Members.Any(m => m.UserId == CurrentUserId));
    }

    private bool IsCurrentUserMember(Conversation conversation)
    {
        return conversation.Members.Any(x => x.UserId == CurrentUserId);
    }

    private static bool IsGroupConversation(Conversation conversation)
    {
        return string.Equals(conversation.Type, "Group", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeConversationType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "private" or "single" => "Single",
            "group" => "Group",
            _ => null
        };
    }

    private static bool IsDissolved(Conversation conversation)
    {
        return string.Equals(conversation.Status, StatusDissolved, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanEditName(Conversation conversation, Guid userId, IReadOnlyList<Guid> adminIds)
    {
        if (conversation.CreatedBy == userId)
        {
            return true;
        }

        if (string.Equals(conversation.EditNamePermission, PermissionAll, StringComparison.OrdinalIgnoreCase))
        {
            return conversation.Members.Any(x => x.UserId == userId);
        }

        return string.Equals(conversation.EditNamePermission, PermissionAdmin, StringComparison.OrdinalIgnoreCase) &&
               adminIds.Contains(userId);
    }

    private static bool CanInviteMembers(Conversation conversation, Guid userId, IReadOnlyList<Guid> adminIds)
    {
        if (conversation.CreatedBy == userId)
        {
            return true;
        }

        return string.Equals(conversation.InvitePermission, PermissionAll, StringComparison.OrdinalIgnoreCase)
            ? conversation.Members.Any(x => x.UserId == userId)
            : adminIds.Contains(userId);
    }

    private static bool CanRemoveMembers(Conversation conversation, Guid userId, IReadOnlyList<Guid> adminIds)
    {
        if (conversation.CreatedBy == userId)
        {
            return true;
        }

        return string.Equals(conversation.KickPermission, PermissionAll, StringComparison.OrdinalIgnoreCase)
            ? conversation.Members.Any(x => x.UserId == userId)
            : adminIds.Contains(userId);
    }

    private static bool TryNormalizePermission(string? value, string currentValue, out string normalized)
    {
        if (value is null)
        {
            normalized = NormalizePermission(currentValue, PermissionAdmin);
            return true;
        }

        normalized = value.Trim().ToLowerInvariant();
        return normalized is PermissionAll or PermissionAdmin;
    }

    private static string NormalizePermission(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is PermissionAll or PermissionAdmin ? normalized : fallback;
    }

    private static IReadOnlyList<Guid> GetAdminIds(Conversation conversation)
    {
        if (string.IsNullOrWhiteSpace(conversation.AdminIdsJson))
        {
            return Array.Empty<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(conversation.AdminIdsJson) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }

    private static void SetAdminIds(Conversation conversation, IEnumerable<Guid> adminIds)
    {
        conversation.AdminIdsJson = JsonSerializer.Serialize(adminIds.Distinct().ToList());
    }

    private async Task PublishConversationUpdatedAsync(Conversation conversation)
    {
        if (hubContext is null)
        {
            return;
        }

        foreach (var member in conversation.Members)
        {
            await hubContext.Clients.Group($"user:{member.UserId}")
                .SendAsync("ConversationUpdated", ToConversationItem(conversation, member.UserId));
        }
    }

    private async Task PublishConversationRemovedAsync(Guid conversationId, Guid userId)
    {
        if (hubContext is null)
        {
            return;
        }

        await hubContext.Clients.Group($"user:{userId}")
            .SendAsync("ConversationRemoved", new { ConversationId = conversationId });
    }

    private async Task PublishConversationDissolvedAsync(Conversation conversation)
    {
        if (hubContext is null)
        {
            return;
        }

        foreach (var member in conversation.Members)
        {
            await hubContext.Clients.Group($"user:{member.UserId}")
                .SendAsync("ConversationDissolved", new { ConversationId = conversation.Id });
        }
    }

    private async Task PublishReactionUpdatedAsync(Message message)
    {
        if (hubContext is null)
        {
            return;
        }

        await hubContext.Clients.Group($"conversation:{message.ConversationId}")
            .SendAsync("MessageReactionUpdated", ToMessageItem(message));
    }

    private static object ToSettings(Conversation conversation)
    {
        return new
        {
            InvitePermission = NormalizePermission(conversation.InvitePermission, PermissionAll),
            KickPermission = NormalizePermission(conversation.KickPermission, PermissionAdmin),
            EditNamePermission = NormalizePermission(conversation.EditNamePermission, PermissionAdmin)
        };
    }

    private static object ToConversationItem(Conversation conversation, Guid currentUserId)
    {
        var member = conversation.Members.FirstOrDefault(x => x.UserId == currentUserId);
        var lastReadAt = member?.LastReadMessageId is null
            ? member?.JoinedAt ?? conversation.CreatedAt
            : conversation.Messages.FirstOrDefault(x => x.Id == member.LastReadMessageId)?.CreatedAt ?? member.JoinedAt;
        var lastMessage = conversation.Messages.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var title = !IsGroupConversation(conversation)
            ? conversation.Members.FirstOrDefault(x => x.UserId != currentUserId)?.User.RealName ?? conversation.Title
            : conversation.Title;

        return new
        {
            conversation.Id,
            conversation.Type,
            Title = title,
            conversation.Avatar,
            Status = string.IsNullOrWhiteSpace(conversation.Status) ? StatusActive : conversation.Status,
            OwnerId = conversation.CreatedBy,
            AdminIds = GetAdminIds(conversation),
            Settings = ToSettings(conversation),
            conversation.Announcement,
            conversation.AnnouncementUpdatedAt,
            Members = conversation.Members.Select(x => new
            {
                x.UserId,
                x.User.RealName,
                x.User.Username,
                Avatar = x.User.Profile?.AvatarUrl
            }),
            LastMessage = lastMessage is null ? null : ToMessageItem(lastMessage),
            UnreadCount = conversation.Messages.Count(x => x.SenderId != currentUserId && x.CreatedAt > lastReadAt),
            conversation.CreatedAt
        };
    }

    private static object ToMessageItem(Message message)
    {
        return new
        {
            message.Id,
            message.ConversationId,
            message.SenderId,
            SenderName = message.Sender?.RealName,
            message.Content,
            message.MessageType,
            message.FileId,
            FileName = message.File?.FileName,
            message.IsRecalled,
            message.CreatedAt,
            MentionUserIds = GetMentionUserIds(message),
            Reactions = message.Reactions.OrderBy(x => x.CreatedAt).Select(x => new
            {
                x.UserId,
                UserName = x.User.RealName,
                x.ReactionType,
                x.CreatedAt
            })
        };
    }

    private static IReadOnlyList<Guid> GetMentionUserIds(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.MentionUserIdsJson))
        {
            return Array.Empty<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(message.MentionUserIdsJson) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }

    private static string? NormalizeReactionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 32 ? normalized : null;
    }
}
