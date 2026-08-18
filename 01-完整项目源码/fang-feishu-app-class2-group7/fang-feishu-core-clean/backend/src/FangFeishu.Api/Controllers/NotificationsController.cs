using FangFeishu.Api.Common;
using FangFeishu.Api.Data;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(
    AppDbContext db,
    IRealtimeEventPublisher? realtimePublisher = null) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? unreadOnly)
    {
        var query = db.Notifications.Where(x => x.UserId == CurrentUserId);
        if (unreadOnly == true)
        {
            query = query.Where(x => !x.IsRead);
        }

        var items = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return OkData(items.Select(x => new
        {
            x.Id,
            x.Title,
            x.Content,
            x.Type,
            x.ResourceType,
            x.ResourceId,
            x.IsRead,
            x.CreatedAt
        }));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await db.Notifications.CountAsync(x => x.UserId == CurrentUserId && !x.IsRead);
        return OkData(new { UnreadCount = count });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (notification is null)
        {
            return Fail(1801, "Notification not found.", StatusCodes.Status404NotFound);
        }

        notification.IsRead = true;
        await db.SaveChangesAsync();
        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUserAsync(
                CurrentUserId,
                RealtimeEventNames.NotificationRead,
                new { notification.Id, notification.IsRead });
        }

        return OkData(new { notification.Id, notification.IsRead });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var notifications = await db.Notifications.Where(x => x.UserId == CurrentUserId && !x.IsRead).ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync();
        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUserAsync(
                CurrentUserId,
                RealtimeEventNames.NotificationsReadAll,
                new { UnreadCount = 0 });
        }

        return OkData(new { UnreadCount = 0 });
    }
}
