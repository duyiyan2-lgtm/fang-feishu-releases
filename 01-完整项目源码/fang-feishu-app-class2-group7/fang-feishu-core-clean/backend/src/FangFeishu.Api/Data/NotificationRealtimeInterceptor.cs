using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FangFeishu.Api.Data;

public sealed class NotificationRealtimeInterceptor(
    IRealtimeEventPublisher realtimePublisher,
    ILogger<NotificationRealtimeInterceptor>? logger = null) : SaveChangesInterceptor
{
    private IReadOnlyList<Notification> pendingNotifications = Array.Empty<Notification>();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CapturePendingNotifications(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CapturePendingNotifications(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        PublishPendingNotificationsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishPendingNotificationsAsync(cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        pendingNotifications = Array.Empty<Notification>();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        pendingNotifications = Array.Empty<Notification>();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void CapturePendingNotifications(DbContext? dbContext)
    {
        pendingNotifications = dbContext is null
            ? Array.Empty<Notification>()
            : dbContext.ChangeTracker
                .Entries<Notification>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .ToList();
    }

    private async Task PublishPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        var notifications = pendingNotifications;
        pendingNotifications = Array.Empty<Notification>();

        foreach (var notification in notifications)
        {
            var payload = new
            {
                notification.Id,
                notification.Title,
                notification.Content,
                notification.Type,
                notification.ResourceType,
                notification.ResourceId,
                notification.IsRead,
                notification.CreatedAt
            };

            try
            {
                await realtimePublisher.SendToUserAsync(
                    notification.UserId,
                    RealtimeEventNames.ReceiveNotification,
                    payload,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                // The database transaction has already committed. A transient
                // realtime delivery failure must not turn a successful write
                // into an HTTP 500 response; clients will receive the stored
                // notification on their next list/reconnect request.
                logger?.LogWarning(
                    exception,
                    "Failed to publish notification {NotificationId} to user {UserId}.",
                    notification.Id,
                    notification.UserId);
            }
        }
    }
}
