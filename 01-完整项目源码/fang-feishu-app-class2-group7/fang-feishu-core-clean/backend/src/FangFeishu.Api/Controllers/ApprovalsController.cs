using System.Text.Json;
using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/approvals")]
[Authorize]
public sealed class ApprovalsController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var query = ApprovalQuery();
        if (!CurrentUserIsAdmin)
        {
            query = query.Where(x => x.ApplicantId == CurrentUserId ||
                x.Template != null && x.Template.Steps.Any(step =>
                    step.StepOrder == x.CurrentStep && step.ApproverId == CurrentUserId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return OkData(items.Select(ToApprovalItem));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(ApprovalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return Fail(2003, "Type, title and content are required.");
        }

        ApprovalTemplate? template = null;
        if (request.TemplateId.HasValue)
        {
            template = await LoadTemplateAsync(request.TemplateId.Value);
            if (template is null || !template.IsActive)
            {
                return Fail(2004, "Approval template not found or inactive.", StatusCodes.Status404NotFound);
            }

            if (template.Steps.Count == 0)
            {
                return Fail(2005, "Approval template has no approver steps.");
            }
        }

        var ccUserIds = await ResolveCcUsersAsync(request.CcUserIds);
        if (ccUserIds is null)
        {
            return Fail(2011, "Some copied users do not exist or are disabled.");
        }

        var item = new ApprovalInstance
        {
            ApplicantId = CurrentUserId,
            Type = request.Type.Trim(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            TemplateId = template?.Id,
            CcUserIdsJson = JsonSerializer.Serialize(ccUserIds),
            CurrentStep = 1,
            Status = "Pending"
        };

        db.ApprovalInstances.Add(item);
        if (template is not null)
        {
            var firstStep = template.Steps.OrderBy(x => x.StepOrder).First();
            AddApprovalNotification(firstStep.ApproverId, "New approval", item);
        }
        else
        {
            var admins = await db.Users
                .Where(x => x.UserRoles.Any(role => role.Role.RoleCode == AppRoles.Admin))
                .Select(x => x.Id)
                .ToListAsync();
            foreach (var adminId in admins)
            {
                AddApprovalNotification(adminId, "New approval", item);
            }
        }

        foreach (var ccUserId in ccUserIds)
        {
            AddApprovalNotification(ccUserId, "Approval copied to you", item);
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "Submit", item.Id.ToString(), HttpContext);
        return CreatedData(ToApprovalItem((await LoadApprovalAsync(item.Id))!));
    }

    [HttpPatch("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, ApprovalActionRequest request)
    {
        return HandleAction(id, "Approved", "Approve", request.Comment);
    }

    [HttpPatch("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, ApprovalActionRequest request)
    {
        return HandleAction(id, "Rejected", "Reject", request.Comment);
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var item = await LoadApprovalAsync(id);
        if (item is null)
        {
            return Fail(2001, "Approval not found.", StatusCodes.Status404NotFound);
        }

        if (item.ApplicantId != CurrentUserId)
        {
            return Fail(2006, "Only applicant can withdraw approval.", StatusCodes.Status403Forbidden);
        }

        if (item.Status != "Pending")
        {
            return Fail(2002, "Only pending approval can be withdrawn.");
        }

        item.Status = "Withdrawn";
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "Withdraw", item.Id.ToString(), HttpContext);
        return OkData(ToApprovalItem(item));
    }

    [HttpPost("{id:guid}/remind")]
    public async Task<IActionResult> Remind(Guid id)
    {
        var item = await LoadApprovalAsync(id);
        if (item is null)
        {
            return Fail(2001, "Approval not found.", StatusCodes.Status404NotFound);
        }

        if (item.ApplicantId != CurrentUserId || item.Status != "Pending")
        {
            return Fail(2007, "Only applicant can remind a pending approval.", StatusCodes.Status403Forbidden);
        }

        var approverIds = GetCurrentApproverIds(item);
        foreach (var approverId in approverIds)
        {
            AddApprovalNotification(approverId, "Approval reminder", item);
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "Remind", item.Id.ToString(), HttpContext);
        return OkData(new { item.Id, RemindedApproverCount = approverIds.Count });
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates([FromQuery] bool? activeOnly)
    {
        var query = db.ApprovalTemplates
            .Include(x => x.Creator)
            .Include(x => x.Steps).ThenInclude(x => x.Approver)
            .AsQueryable();
        if (activeOnly == true)
        {
            query = query.Where(x => x.IsActive);
        }

        var templates = await query.OrderBy(x => x.Name).ToListAsync();
        return OkData(templates.Select(ToTemplateItem));
    }

    [HttpGet("templates/{id:guid}")]
    public async Task<IActionResult> TemplateDetail(Guid id)
    {
        var template = await LoadTemplateAsync(id);
        return template is null
            ? Fail(2004, "Approval template not found.", StatusCodes.Status404NotFound)
            : OkData(ToTemplateItem(template));
    }

    [HttpPost("templates")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateTemplate(ApprovalTemplateRequest request)
    {
        var approvers = await ResolveApproversAsync(request.ApproverUserIds);
        if (approvers is null)
        {
            return Fail(2008, "Template needs at least one active approver.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
        {
            return Fail(2009, "Template name and type are required.");
        }

        var template = new ApprovalTemplate
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = NormalizeOptional(request.Description),
            CreatedBy = CurrentUserId,
            IsActive = request.IsActive
        };
        for (var index = 0; index < approvers.Count; index++)
        {
            template.Steps.Add(new ApprovalTemplateStep
            {
                Template = template,
                ApproverId = approvers[index].Id,
                StepOrder = index + 1
            });
        }

        db.ApprovalTemplates.Add(template);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "CreateTemplate", template.Id.ToString(), HttpContext);
        return CreatedData(ToTemplateItem((await LoadTemplateAsync(template.Id))!));
    }

    [HttpPut("templates/{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateTemplate(Guid id, ApprovalTemplateRequest request)
    {
        var template = await LoadTemplateAsync(id);
        if (template is null)
        {
            return Fail(2004, "Approval template not found.", StatusCodes.Status404NotFound);
        }

        var approvers = await ResolveApproversAsync(request.ApproverUserIds);
        if (approvers is null)
        {
            return Fail(2008, "Template needs at least one active approver.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
        {
            return Fail(2009, "Template name and type are required.");
        }

        template.Name = request.Name.Trim();
        template.Type = request.Type.Trim();
        template.Description = NormalizeOptional(request.Description);
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        db.ApprovalTemplateSteps.RemoveRange(template.Steps);
        for (var index = 0; index < approvers.Count; index++)
        {
            db.ApprovalTemplateSteps.Add(new ApprovalTemplateStep
            {
                TemplateId = template.Id,
                ApproverId = approvers[index].Id,
                StepOrder = index + 1
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "UpdateTemplate", template.Id.ToString(), HttpContext);
        return OkData(ToTemplateItem((await LoadTemplateAsync(template.Id))!));
    }

    [HttpDelete("templates/{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var template = await db.ApprovalTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (template is null)
        {
            return Fail(2004, "Approval template not found.", StatusCodes.Status404NotFound);
        }

        if (await db.ApprovalInstances.AnyAsync(x => x.TemplateId == id))
        {
            return Fail(2010, "Template already has approval instances and cannot be deleted.");
        }

        db.ApprovalTemplates.Remove(template);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", "DeleteTemplate", id.ToString(), HttpContext);
        return OkData(new { Id = id });
    }

    private async Task<IActionResult> HandleAction(Guid id, string finalStatus, string action, string? comment)
    {
        var item = await LoadApprovalAsync(id);
        if (item is null)
        {
            return Fail(2001, "Approval not found.", StatusCodes.Status404NotFound);
        }

        if (item.Status != "Pending")
        {
            return Fail(2002, "Approval has already been handled.");
        }

        var approverIds = GetCurrentApproverIds(item);
        if (!CurrentUserIsAdmin && !approverIds.Contains(CurrentUserId))
        {
            return Fail(2006, "No approval permission.", StatusCodes.Status403Forbidden);
        }

        db.ApprovalRecords.Add(new ApprovalRecord
        {
            InstanceId = item.Id,
            ApproverId = CurrentUserId,
            Action = action,
            Comment = NormalizeOptional(comment)
        });

        if (finalStatus == "Rejected")
        {
            item.Status = "Rejected";
            AddApprovalNotification(item.ApplicantId, "Approval rejected", item);
        }
        else if (item.Template is not null)
        {
            var nextStep = item.Template.Steps.OrderBy(x => x.StepOrder).FirstOrDefault(x => x.StepOrder > item.CurrentStep);
            if (nextStep is null)
            {
                item.Status = "Approved";
                AddApprovalNotification(item.ApplicantId, "Approval approved", item);
            }
            else
            {
                item.CurrentStep = nextStep.StepOrder;
                AddApprovalNotification(nextStep.ApproverId, "New approval", item);
            }
        }
        else
        {
            item.Status = "Approved";
            AddApprovalNotification(item.ApplicantId, "Approval approved", item);
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Approval", action, item.Id.ToString(), HttpContext);
        return OkData(ToApprovalItem((await LoadApprovalAsync(item.Id))!));
    }

    private IQueryable<ApprovalInstance> ApprovalQuery()
    {
        return db.ApprovalInstances
            .Include(x => x.Applicant)
            .Include(x => x.Records).ThenInclude(x => x.Approver)
            .Include(x => x.Template!).ThenInclude(x => x!.Steps).ThenInclude(x => x.Approver);
    }

    private Task<ApprovalInstance?> LoadApprovalAsync(Guid id)
    {
        return ApprovalQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    private Task<ApprovalTemplate?> LoadTemplateAsync(Guid id)
    {
        return db.ApprovalTemplates
            .Include(x => x.Creator)
            .Include(x => x.Steps).ThenInclude(x => x.Approver)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<List<User>?> ResolveApproversAsync(IReadOnlyList<Guid>? requestedApproverIds)
    {
        var userIds = (requestedApproverIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (userIds.Count == 0)
        {
            return null;
        }

        var users = await db.Users.Where(x => userIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        return users.Count == userIds.Count ? userIds.Select(id => users.Single(x => x.Id == id)).ToList() : null;
    }

    private async Task<List<Guid>?> ResolveCcUsersAsync(IReadOnlyList<Guid>? requestedCcUserIds)
    {
        var userIds = (requestedCcUserIds ?? Array.Empty<Guid>())
            .Where(x => x != CurrentUserId)
            .Distinct()
            .ToList();
        var activeUserIds = await db.Users
            .Where(x => userIds.Contains(x.Id) && x.Status == "Active")
            .Select(x => x.Id)
            .ToListAsync();
        return activeUserIds.Count == userIds.Count ? userIds : null;
    }

    private IReadOnlyList<Guid> GetCurrentApproverIds(ApprovalInstance item)
    {
        if (item.Template is not null)
        {
            return item.Template.Steps
                .Where(x => x.StepOrder == item.CurrentStep)
                .Select(x => x.ApproverId)
                .ToList();
        }

        return db.Users
            .Where(x => x.UserRoles.Any(role => role.Role.RoleCode == AppRoles.Admin))
            .Select(x => x.Id)
            .ToList();
    }

    private void AddApprovalNotification(Guid userId, string title, ApprovalInstance item)
    {
        if (userId == CurrentUserId)
        {
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = item.Title,
            Type = "Approval",
            ResourceType = "Approval",
            ResourceId = item.Id
        });
    }

    private static object ToApprovalItem(ApprovalInstance item)
    {
        var currentApprover = item.Template?.Steps.FirstOrDefault(x => x.StepOrder == item.CurrentStep)?.Approver;
        return new
        {
            item.Id,
            item.ApplicantId,
            ApplicantName = item.Applicant.RealName,
            item.Type,
            item.Title,
            item.Content,
            item.Status,
            item.TemplateId,
            CcUserIds = GetCcUserIds(item),
            TemplateName = item.Template?.Name,
            item.CurrentStep,
            CurrentApproverId = currentApprover?.Id,
            CurrentApproverName = currentApprover?.RealName,
            item.CreatedAt,
            Records = item.Records.OrderBy(x => x.CreatedAt).Select(x => new
            {
                x.Id,
                x.ApproverId,
                ApproverName = x.Approver.RealName,
                x.Action,
                x.Comment,
                x.CreatedAt
            })
        };
    }

    private static object ToTemplateItem(ApprovalTemplate template)
    {
        return new
        {
            template.Id,
            template.Name,
            template.Type,
            template.Description,
            template.CreatedBy,
            CreatorName = template.Creator.RealName,
            template.IsActive,
            template.CreatedAt,
            template.UpdatedAt,
            Steps = template.Steps.OrderBy(x => x.StepOrder).Select(x => new
            {
                x.StepOrder,
                x.ApproverId,
                ApproverName = x.Approver.RealName
            })
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<Guid> GetCcUserIds(ApprovalInstance item)
    {
        if (string.IsNullOrWhiteSpace(item.CcUserIdsJson))
        {
            return Array.Empty<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(item.CcUserIdsJson) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }
}
