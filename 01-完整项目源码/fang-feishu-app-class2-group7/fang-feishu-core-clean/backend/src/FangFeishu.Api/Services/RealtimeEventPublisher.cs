using FangFeishu.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FangFeishu.Api.Services;

public static class RealtimeEventNames
{
    public const string ReceiveNotification = "ReceiveNotification";
    public const string NotificationRead = "NotificationRead";
    public const string NotificationsReadAll = "NotificationsReadAll";
    public const string FriendRequestReceived = "FriendRequestReceived";
    public const string FriendRequestAccepted = "FriendRequestAccepted";
    public const string FriendRequestRejected = "FriendRequestRejected";
    public const string FriendRemoved = "FriendRemoved";
    public const string MeetingInvited = "MeetingInvited";
    public const string MeetingEnded = "MeetingEnded";
}

public interface IRealtimeEventPublisher
{
    Task SendToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string eventName, object payload, CancellationToken cancellationToken = default);
}

public sealed class RealtimeEventPublisher(
    IHubContext<ImHub> hubContext,
    ILogger<RealtimeEventPublisher> logger) : IRealtimeEventPublisher
{
    public async Task SendToUserAsync(
        Guid userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients.Group($"user:{userId}")
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception exception)
        {
            // Realtime delivery is best effort. The database mutation has already
            // succeeded, so a transient SignalR failure must not turn the API into
            // an apparent failure and tempt the client to submit the mutation again.
            logger.LogWarning(
                exception,
                "Failed to publish realtime event {EventName} to user {UserId}.",
                eventName,
                userId);
        }
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
