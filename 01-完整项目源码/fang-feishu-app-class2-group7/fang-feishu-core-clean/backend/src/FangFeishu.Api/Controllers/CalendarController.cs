using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/calendar/events")]
[Authorize]
public sealed class CalendarController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var query = EventQuery().Where(x =>
            x.UserId == CurrentUserId || x.Attendees.Any(attendee => attendee.UserId == CurrentUserId));
        if (from.HasValue)
        {
            var fromUtc = ToUtcDateTime(from.Value);
            query = query.Where(x => x.EndTime >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = ToUtcDateTime(to.Value);
            query = query.Where(x => x.StartTime <= toUtc);
        }

        var events = await query.OrderBy(x => x.StartTime).ToListAsync();
        return OkData(events.Select(ToEventItem));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CalendarEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Fail(1903, "Event title is required.");
        }

        if (request.EndTime < request.StartTime)
        {
            return Fail(1902, "End time must be later than start time.");
        }

        var invitedUsers = await ResolveAttendeesAsync(request.AttendeeUserIds, CurrentUserId);
        if (invitedUsers is null)
        {
            return Fail(1904, "Some attendees do not exist or are disabled.");
        }

        if (!TryNormalizeRecurrence(request.RecurrenceType, "None", out var recurrenceType))
        {
            return Fail(1906, "Recurrence type must be None, Daily, Weekly or Monthly.");
        }

        if (recurrenceType != "None" && request.RecurrenceUntil.HasValue && request.RecurrenceUntil < request.StartTime)
        {
            return Fail(1907, "Recurrence end time must not be earlier than start time.");
        }

        var item = new CalendarEvent
        {
            UserId = CurrentUserId,
            Title = request.Title.Trim(),
            StartTime = ToUtcDateTime(request.StartTime),
            EndTime = ToUtcDateTime(request.EndTime),
            Location = NormalizeOptional(request.Location),
            Description = NormalizeOptional(request.Description),
            RecurrenceType = recurrenceType,
            RecurrenceUntil = recurrenceType == "None" ? null : request.RecurrenceUntil?.UtcDateTime
        };
        db.CalendarEvents.Add(item);
        AddAttendees(item, invitedUsers);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Calendar", "Create", item.Id.ToString(), HttpContext);
        return CreatedData(ToEventItem((await LoadEventAsync(item.Id))!));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CalendarEventRequest request)
    {
        var item = await LoadEventAsync(id);
        if (item is null || item.UserId != CurrentUserId)
        {
            return Fail(1901, "Calendar event not found.", StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Fail(1903, "Event title is required.");
        }

        if (request.EndTime < request.StartTime)
        {
            return Fail(1902, "End time must be later than start time.");
        }

        if (!TryNormalizeRecurrence(request.RecurrenceType, item.RecurrenceType, out var recurrenceType))
        {
            return Fail(1906, "Recurrence type must be None, Daily, Weekly or Monthly.");
        }

        if (recurrenceType != "None" && request.RecurrenceUntil.HasValue && request.RecurrenceUntil < request.StartTime)
        {
            return Fail(1907, "Recurrence end time must not be earlier than start time.");
        }

        if (request.AttendeeUserIds is not null)
        {
            var invitedUsers = await ResolveAttendeesAsync(request.AttendeeUserIds, item.UserId);
            if (invitedUsers is null)
            {
                return Fail(1904, "Some attendees do not exist or are disabled.");
            }

            db.CalendarEventAttendees.RemoveRange(item.Attendees);
            item.Attendees.Clear();
            AddAttendees(item, invitedUsers);
        }

        item.Title = request.Title.Trim();
        item.StartTime = ToUtcDateTime(request.StartTime);
        item.EndTime = ToUtcDateTime(request.EndTime);
        item.Location = NormalizeOptional(request.Location);
        item.Description = NormalizeOptional(request.Description);
        item.RecurrenceType = recurrenceType;
        item.RecurrenceUntil = recurrenceType == "None" ? null : request.RecurrenceUntil?.UtcDateTime ?? item.RecurrenceUntil;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Calendar", "Update", item.Id.ToString(), HttpContext);
        return OkData(ToEventItem((await LoadEventAsync(item.Id))!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await db.CalendarEvents.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (item is null)
        {
            return Fail(1901, "Calendar event not found.", StatusCodes.Status404NotFound);
        }

        db.CalendarEvents.Remove(item);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Calendar", "Delete", item.Id.ToString(), HttpContext);
        return OkData(new { id });
    }

    [HttpPatch("{id:guid}/attendance")]
    public async Task<IActionResult> UpdateAttendance(Guid id, CalendarAttendanceRequest request)
    {
        var status = NormalizeAttendanceStatus(request.Status);
        if (status is null)
        {
            return Fail(1905, "Attendance status must be Accepted, Declined or Tentative.");
        }

        var attendee = await db.CalendarEventAttendees
            .Include(x => x.CalendarEvent)
            .FirstOrDefaultAsync(x => x.CalendarEventId == id && x.UserId == CurrentUserId);
        if (attendee is null)
        {
            return Fail(1901, "Calendar invitation not found.", StatusCodes.Status404NotFound);
        }

        attendee.Status = status;
        attendee.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Calendar", "UpdateAttendance", id.ToString(), HttpContext);
        return OkData(new { attendee.CalendarEventId, attendee.UserId, attendee.Status, attendee.RespondedAt });
    }

    [HttpGet("{id:guid}/occurrences")]
    public async Task<IActionResult> Occurrences(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var item = await LoadEventAsync(id);
        if (item is null || (item.UserId != CurrentUserId && !item.Attendees.Any(x => x.UserId == CurrentUserId)))
        {
            return Fail(1901, "Calendar event not found.", StatusCodes.Status404NotFound);
        }

        var rangeStart = from?.UtcDateTime ?? item.StartTime;
        var rangeEnd = to?.UtcDateTime ?? item.RecurrenceUntil ?? item.StartTime.AddDays(90);
        if (rangeEnd < rangeStart)
        {
            return Fail(1902, "End time must be later than start time.");
        }

        var occurrences = BuildOccurrences(item, rangeStart, rangeEnd);
        return OkData(occurrences);
    }

    [HttpGet("free-busy")]
    public async Task<IActionResult> FreeBusy(
        [FromQuery] IReadOnlyList<Guid>? userIds,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        if (to <= from)
        {
            return Fail(1902, "End time must be later than start time.");
        }

        var selectedUserIds = (userIds ?? Array.Empty<Guid>())
            .Append(CurrentUserId)
            .Distinct()
            .Take(50)
            .ToList();
        var fromUtc = ToUtcDateTime(from);
        var toUtc = ToUtcDateTime(to);
        var events = await EventQuery()
            .Where(x => x.EndTime >= fromUtc && x.StartTime <= toUtc &&
                (selectedUserIds.Contains(x.UserId) || x.Attendees.Any(attendee => selectedUserIds.Contains(attendee.UserId))))
            .ToListAsync();

        var slots = events.SelectMany(item => item.Attendees
                .Select(x => x.UserId)
                .Append(item.UserId)
                .Where(selectedUserIds.Contains)
                .Distinct()
                .Select(userId => new { UserId = userId, item.StartTime, item.EndTime, Status = "Busy" }))
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.StartTime);
        return OkData(slots);
    }

    private IQueryable<CalendarEvent> EventQuery()
    {
        return db.CalendarEvents
            .Include(x => x.User)
            .Include(x => x.Attendees).ThenInclude(x => x.User);
    }

    private Task<CalendarEvent?> LoadEventAsync(Guid id)
    {
        return EventQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<List<User>?> ResolveAttendeesAsync(IReadOnlyList<Guid>? requestedUserIds, Guid organizerId)
    {
        var userIds = (requestedUserIds ?? Array.Empty<Guid>())
            .Where(x => x != organizerId)
            .Distinct()
            .ToList();
        var users = await db.Users.Where(x => userIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        return users.Count == userIds.Count ? users : null;
    }

    private void AddAttendees(CalendarEvent item, IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            var attendee = new CalendarEventAttendee
            {
                CalendarEventId = item.Id,
                UserId = user.Id,
                User = user
            };
            db.CalendarEventAttendees.Add(attendee);
            item.Attendees.Add(attendee);
            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Calendar invitation",
                Content = item.Title,
                Type = "Calendar",
                ResourceType = "CalendarEvent",
                ResourceId = item.Id
            });
        }
    }

    private static object ToEventItem(CalendarEvent item)
    {
        return new
        {
            item.Id,
            item.UserId,
            OrganizerName = item.User.RealName,
            item.Title,
            item.StartTime,
            item.EndTime,
            item.Location,
            item.Description,
            item.RecurrenceType,
            item.RecurrenceUntil,
            Attendees = item.Attendees.OrderBy(x => x.InvitedAt).Select(x => new
            {
                x.UserId,
                UserName = x.User.RealName,
                x.Status,
                x.InvitedAt,
                x.RespondedAt
            })
        };
    }

    private static string? NormalizeAttendanceStatus(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "accepted" => "Accepted",
            "declined" => "Declined",
            "tentative" => "Tentative",
            _ => null
        };
    }

    private static bool TryNormalizeRecurrence(string? value, string currentValue, out string recurrenceType)
    {
        if (value is null)
        {
            recurrenceType = string.IsNullOrWhiteSpace(currentValue) ? "None" : currentValue;
            return true;
        }

        recurrenceType = value.Trim().ToLowerInvariant() switch
        {
            "none" => "None",
            "daily" => "Daily",
            "weekly" => "Weekly",
            "monthly" => "Monthly",
            _ => string.Empty
        };
        return recurrenceType.Length > 0;
    }

    private static IEnumerable<object> BuildOccurrences(CalendarEvent item, DateTime rangeStart, DateTime rangeEnd)
    {
        var recurrenceUntil = item.RecurrenceUntil ?? rangeEnd;
        var occurrenceStart = item.StartTime;
        var occurrenceEnd = item.EndTime;
        var occurrences = new List<object>();
        for (var index = 0; index < 366 && occurrenceStart <= rangeEnd && occurrenceStart <= recurrenceUntil; index++)
        {
            if (occurrenceEnd >= rangeStart)
            {
                occurrences.Add(new
                {
                    EventId = item.Id,
                    OccurrenceIndex = index,
                    item.Title,
                    StartTime = occurrenceStart,
                    EndTime = occurrenceEnd,
                    item.Location,
                    item.Description
                });
            }

            switch (item.RecurrenceType)
            {
                case "Daily":
                    occurrenceStart = occurrenceStart.AddDays(1);
                    occurrenceEnd = occurrenceEnd.AddDays(1);
                    break;
                case "Weekly":
                    occurrenceStart = occurrenceStart.AddDays(7);
                    occurrenceEnd = occurrenceEnd.AddDays(7);
                    break;
                case "Monthly":
                    occurrenceStart = occurrenceStart.AddMonths(1);
                    occurrenceEnd = occurrenceEnd.AddMonths(1);
                    break;
                default:
                    return occurrences;
            }
        }

        return occurrences;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime ToUtcDateTime(DateTimeOffset value)
    {
        return DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Utc);
    }
}
