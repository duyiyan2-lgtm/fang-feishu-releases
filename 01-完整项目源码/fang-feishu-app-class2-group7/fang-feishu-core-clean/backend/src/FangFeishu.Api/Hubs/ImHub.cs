using System.Text.Json;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Hubs;

[Authorize]
public sealed class ImHub(AppDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        var isMember = await db.Conversations.AnyAsync(x =>
            x.Id == conversationId &&
            x.Status != "dissolved" &&
            x.Members.Any(member => member.UserId == userId));
        if (!isMember)
        {
            throw new HubException("You are not a conversation member.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = GetUserId();
        var canSend = await db.Conversations.AnyAsync(x =>
            x.Id == request.ConversationId &&
            x.Status != "dissolved" &&
            x.Members.Any(member => member.UserId == userId));
        if (!canSend)
        {
            throw new HubException("You are not a conversation member.");
        }

        var members = await db.ConversationMembers
            .Where(x => x.ConversationId == request.ConversationId)
            .ToListAsync();

        if (members.All(x => x.UserId != userId))
        {
            throw new HubException("You are not a conversation member.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new HubException("Message content is required.");
        }

        var memberIds = members.Select(x => x.UserId).ToHashSet();
        var mentionUserIds = (request.MentionUserIds ?? Array.Empty<Guid>())
            .Where(x => x != userId)
            .Distinct()
            .ToList();
        if (mentionUserIds.Any(x => !memberIds.Contains(x)))
        {
            throw new HubException("Mentioned users must be conversation members.");
        }

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = userId,
            Content = request.Content,
            MessageType = request.MessageType,
            FileId = request.FileId,
            MentionUserIdsJson = JsonSerializer.Serialize(mentionUserIds)
        };

        db.Messages.Add(message);
        foreach (var member in members.Where(x => x.UserId != userId))
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

        var payload = new
        {
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.MessageType,
            message.FileId,
            MentionUserIds = mentionUserIds,
            message.IsRecalled,
            message.CreatedAt
        };

        await Clients.Group($"conversation:{request.ConversationId}").SendAsync("ReceiveMessage", payload);
        foreach (var member in members)
        {
            await Clients.Group($"user:{member.UserId}").SendAsync("ReceiveMessage", payload);
        }
    }

    private Guid GetUserId()
    {
        return Guid.Parse(Context.UserIdentifier ?? throw new HubException("Missing user id."));
    }
}
