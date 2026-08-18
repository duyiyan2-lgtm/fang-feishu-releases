using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/tasks")]
[Authorize]
public sealed class TasksController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    private const string Todo = "Todo";
    private const string InProgress = "InProgress";
    private const string Completed = "Completed";

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? scope, [FromQuery] string? status)
    {
        var query = db.WorkTasks
            .Include(x => x.Creator)
            .Include(x => x.Assignee)
            .AsQueryable();

        switch (scope?.Trim().ToLowerInvariant())
        {
            case "assigned":
                query = query.Where(x => x.AssigneeId == CurrentUserId);
                break;
            case "created":
                query = query.Where(x => x.CreatorId == CurrentUserId);
                break;
            case null:
            case "":
            case "all":
                query = query.Where(x => x.CreatorId == CurrentUserId || x.AssigneeId == CurrentUserId);
                break;
            default:
                return Fail(2201, "Scope value must be all, assigned or created.");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryNormalizeStatus(status, out var normalizedStatus))
            {
                return Fail(2202, "Status value must be Todo, InProgress or Completed.");
            }

            query = query.Where(x => x.Status == normalizedStatus);
        }

        var tasks = await query
            .OrderBy(x => x.Status == Completed)
            .ThenBy(x => x.DueAt == null)
            .ThenBy(x => x.DueAt)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync();
        return OkData(tasks.Select(ToTaskItem));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Fail(2203, "Task title is required.");
        }

        if (request.AssigneeId.HasValue && !await IsActiveUserAsync(request.AssigneeId.Value))
        {
            return Fail(2204, "Assignee does not exist or is disabled.");
        }

        var task = new WorkTask
        {
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            CreatorId = CurrentUserId,
            AssigneeId = request.AssigneeId,
            DueAt = request.DueAt?.UtcDateTime
        };

        db.WorkTasks.Add(task);
        AddAssignmentNotification(task);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Task", "Create", task.Id.ToString(), HttpContext);

        return CreatedData(ToTaskItem((await LoadTaskAsync(task.Id))!));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Fail(2205, "Task not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(task))
        {
            return Fail(2206, "No task permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(ToTaskItem(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Fail(2205, "Task not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(task))
        {
            return Fail(2206, "Only task creator or admin can edit task details.", StatusCodes.Status403Forbidden);
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Fail(2203, "Task title is required.");
            }

            task.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            task.Description = NormalizeDescription(request.Description);
        }

        if (request.AssigneeId.HasValue && request.AssigneeId != task.AssigneeId)
        {
            if (!await IsActiveUserAsync(request.AssigneeId.Value))
            {
                return Fail(2204, "Assignee does not exist or is disabled.");
            }

            task.AssigneeId = request.AssigneeId;
            AddAssignmentNotification(task);
        }

        if (request.DueAt.HasValue)
        {
            task.DueAt = request.DueAt.Value.UtcDateTime;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Task", "Update", task.Id.ToString(), HttpContext);

        return OkData(ToTaskItem((await LoadTaskAsync(task.Id))!));
    }

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request)
    {
        return SetStatusAsync(id, request.Status, "UpdateStatus");
    }

    [HttpPatch("{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id)
    {
        return SetStatusAsync(id, Completed, "Complete");
    }

    [HttpPatch("{id:guid}/reopen")]
    public Task<IActionResult> Reopen(Guid id)
    {
        return SetStatusAsync(id, Todo, "Reopen");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Fail(2205, "Task not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(task))
        {
            return Fail(2206, "Only task creator or admin can delete task.", StatusCodes.Status403Forbidden);
        }

        db.WorkTasks.Remove(task);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Task", "Delete", id.ToString(), HttpContext);
        return OkData(new { Id = id });
    }

    private async Task<IActionResult> SetStatusAsync(Guid id, string status, string action)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Fail(2205, "Task not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(task))
        {
            return Fail(2206, "No task permission.", StatusCodes.Status403Forbidden);
        }

        if (!TryNormalizeStatus(status, out var normalizedStatus))
        {
            return Fail(2202, "Status value must be Todo, InProgress or Completed.");
        }

        task.Status = normalizedStatus;
        task.CompletedAt = normalizedStatus == Completed ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Task", action, task.Id.ToString(), HttpContext);
        return OkData(ToTaskItem((await LoadTaskAsync(task.Id))!));
    }

    private void AddAssignmentNotification(WorkTask task)
    {
        if (!task.AssigneeId.HasValue || task.AssigneeId == CurrentUserId)
        {
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = task.AssigneeId.Value,
            Title = "Task assigned to you",
            Content = task.Title,
            Type = "Task",
            ResourceType = "Task",
            ResourceId = task.Id
        });
    }

    private Task<bool> IsActiveUserAsync(Guid userId)
    {
        return db.Users.AnyAsync(x => x.Id == userId && x.Status == "Active");
    }

    private Task<WorkTask?> LoadTaskAsync(Guid id)
    {
        return db.WorkTasks
            .Include(x => x.Creator)
            .Include(x => x.Assignee)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private bool CanAccess(WorkTask task)
    {
        return CurrentUserIsAdmin || task.CreatorId == CurrentUserId || task.AssigneeId == CurrentUserId;
    }

    private bool CanManage(WorkTask task)
    {
        return CurrentUserIsAdmin || task.CreatorId == CurrentUserId;
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static bool TryNormalizeStatus(string? value, out string status)
    {
        status = value?.Trim().ToLowerInvariant() switch
        {
            "todo" => Todo,
            "inprogress" => InProgress,
            "completed" => Completed,
            _ => string.Empty
        };
        return status.Length > 0;
    }

    private static object ToTaskItem(WorkTask task)
    {
        return new
        {
            task.Id,
            task.Title,
            task.Description,
            task.CreatorId,
            CreatorName = task.Creator.RealName,
            task.AssigneeId,
            AssigneeName = task.Assignee?.RealName,
            task.Status,
            task.DueAt,
            task.CompletedAt,
            task.CreatedAt,
            task.UpdatedAt
        };
    }
}
