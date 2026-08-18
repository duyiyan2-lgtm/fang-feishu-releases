using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/contacts")]
[Authorize]
public sealed class ContactsController(
    AppDbContext db,
    IRealtimeEventPublisher? realtimePublisher = null) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var friendships = await FriendshipQuery()
            .Where(x => x.Status == "Accepted" && (x.RequesterId == CurrentUserId || x.AddresseeId == CurrentUserId))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return OkData(friendships.Select(x => ToContact(x.RequesterId == CurrentUserId ? x.Addressee : x.Requester)));
    }

    [HttpGet("discover")]
    public async Task<IActionResult> Discover([FromQuery] string? keyword)
    {
        var acceptedIds = await db.Friendships
            .Where(x => x.Status == "Accepted" && (x.RequesterId == CurrentUserId || x.AddresseeId == CurrentUserId))
            .Select(x => x.RequesterId == CurrentUserId ? x.AddresseeId : x.RequesterId)
            .ToListAsync();

        var query = ActiveUsers().Where(x => x.Id != CurrentUserId && !acceptedIds.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x =>
                x.RealName.Contains(value) ||
                x.Username.Contains(value) ||
                (x.Email != null && x.Email.Contains(value)) ||
                (x.Phone != null && x.Phone.Contains(value)));
        }

        var users = await query.OrderBy(x => x.RealName).Take(50).ToListAsync();
        return OkData(users.Select(ToContact));
    }

    [HttpGet("search")]
    public Task<IActionResult> Search([FromQuery] string keyword) => Discover(keyword);

    [HttpGet("requests")]
    public async Task<IActionResult> Requests()
    {
        var requests = await FriendshipQuery()
            .Where(x => x.Status == "Pending" && (x.RequesterId == CurrentUserId || x.AddresseeId == CurrentUserId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return OkData(requests.Select(x => ToFriendRequest(x, CurrentUserId)));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendRequest(CreateFriendRequest request)
    {
        if (request.UserId == CurrentUserId)
        {
            return Fail(1501, "You cannot add yourself as a friend.");
        }

        var target = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId && x.Status == "Active");
        if (target is null)
        {
            return Fail(1401, "Contact not found.", StatusCodes.Status404NotFound);
        }

        var greeting = request.Greeting?.Trim();
        if (greeting?.Length > 280)
        {
            return Fail(1502, "Friend greeting must be at most 280 characters.");
        }

        var friendship = await FriendshipQuery()
            .FirstOrDefaultAsync(x =>
                (x.RequesterId == CurrentUserId && x.AddresseeId == target.Id) ||
                (x.RequesterId == target.Id && x.AddresseeId == CurrentUserId));

        if (friendship?.Status == "Accepted")
        {
            return OkData(ToFriendRequest(friendship, CurrentUserId), "already friends");
        }

        if (friendship?.Status == "Pending")
        {
            return Fail(1503, "A friend request is already pending.", StatusCodes.Status409Conflict);
        }

        if (friendship is null)
        {
            friendship = new Friendship
            {
                RequesterId = CurrentUserId,
                AddresseeId = target.Id,
                Greeting = string.IsNullOrWhiteSpace(greeting) ? null : greeting
            };
            db.Friendships.Add(friendship);
        }
        else
        {
            friendship.RequesterId = CurrentUserId;
            friendship.AddresseeId = target.Id;
            friendship.Status = "Pending";
            friendship.Greeting = string.IsNullOrWhiteSpace(greeting) ? null : greeting;
            friendship.CreatedAt = DateTime.UtcNow;
            friendship.UpdatedAt = DateTime.UtcNow;
        }

        db.Notifications.Add(new Notification
        {
            UserId = target.Id,
            Title = "Friend request",
            Content = "You have received a friend request.",
            Type = "Friend",
            ResourceType = "Friendship",
            ResourceId = friendship.Id
        });
        await db.SaveChangesAsync();

        var created = await FriendshipQuery().SingleAsync(x => x.Id == friendship.Id);
        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUserAsync(
                target.Id,
                RealtimeEventNames.FriendRequestReceived,
                ToFriendRequest(created, target.Id));
        }

        return CreatedData(ToFriendRequest(created, CurrentUserId));
    }

    [HttpPatch("requests/{id:guid}/accept")]
    public Task<IActionResult> Accept(Guid id) => UpdateRequestStatus(id, "Accepted");

    [HttpPatch("requests/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id) => UpdateRequestStatus(id, "Rejected");

    [HttpDelete("friends/{userId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid userId)
    {
        var friendship = await db.Friendships.FirstOrDefaultAsync(x =>
            x.Status == "Accepted" &&
            ((x.RequesterId == CurrentUserId && x.AddresseeId == userId) ||
             (x.RequesterId == userId && x.AddresseeId == CurrentUserId)));
        if (friendship is null)
        {
            return Fail(1504, "Friend relationship not found.", StatusCodes.Status404NotFound);
        }

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync();

        if (realtimePublisher is not null)
        {
            await realtimePublisher.SendToUserAsync(
                CurrentUserId,
                RealtimeEventNames.FriendRemoved,
                new { UserId = userId });
            await realtimePublisher.SendToUserAsync(
                userId,
                RealtimeEventNames.FriendRemoved,
                new { UserId = CurrentUserId });
        }

        return OkData(new { UserId = userId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var user = await ActiveUsers().FirstOrDefaultAsync(x => x.Id == id);
        return user is null
            ? Fail(1401, "Contact not found.", StatusCodes.Status404NotFound)
            : OkData(ToContact(user));
    }

    private async Task<IActionResult> UpdateRequestStatus(Guid id, string status)
    {
        var friendship = await FriendshipQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (friendship is null)
        {
            return Fail(1505, "Friend request not found.", StatusCodes.Status404NotFound);
        }

        if (friendship.AddresseeId != CurrentUserId)
        {
            return Fail(1506, "Only the recipient can process this friend request.", StatusCodes.Status403Forbidden);
        }

        if (friendship.Status != "Pending")
        {
            return Fail(1507, "Friend request has already been processed.", StatusCodes.Status409Conflict);
        }

        friendship.Status = status;
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (realtimePublisher is not null)
        {
            var eventName = status == "Accepted"
                ? RealtimeEventNames.FriendRequestAccepted
                : RealtimeEventNames.FriendRequestRejected;
            await realtimePublisher.SendToUserAsync(
                friendship.RequesterId,
                eventName,
                ToFriendRequest(friendship, friendship.RequesterId));
            await realtimePublisher.SendToUserAsync(
                friendship.AddresseeId,
                eventName,
                ToFriendRequest(friendship, friendship.AddresseeId));
        }

        return OkData(ToFriendRequest(friendship, CurrentUserId));
    }

    private IQueryable<User> ActiveUsers()
    {
        return db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Where(x => x.Status == "Active");
    }

    private IQueryable<Friendship> FriendshipQuery()
    {
        return db.Friendships
            .Include(x => x.Requester).ThenInclude(x => x.Department)
            .Include(x => x.Requester).ThenInclude(x => x.Profile)
            .Include(x => x.Addressee).ThenInclude(x => x.Department)
            .Include(x => x.Addressee).ThenInclude(x => x.Profile);
    }

    private static object ToFriendRequest(Friendship friendship, Guid currentUserId)
    {
        var outgoing = friendship.RequesterId == currentUserId;
        return new
        {
            friendship.Id,
            friendship.Status,
            Direction = outgoing ? "Outgoing" : "Incoming",
            friendship.Greeting,
            friendship.CreatedAt,
            User = ToContact(outgoing ? friendship.Addressee : friendship.Requester)
        };
    }

    private static object ToContact(User user)
    {
        return new
        {
            user.Id,
            user.Username,
            user.RealName,
            user.Email,
            user.Phone,
            user.DepartmentId,
            DepartmentName = user.Department?.Name,
            Position = user.Profile?.Position,
            AvatarUrl = user.Profile?.AvatarUrl,
            WorkPlace = user.Profile?.WorkPlace,
            Bio = user.Profile?.Bio
        };
    }
}
