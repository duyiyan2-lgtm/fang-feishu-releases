using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/wiki")]
[Authorize]
public sealed class WikiController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Fail(2312, "Search keyword is required.");
        }

        var spaces = await db.WikiSpaces
            .Include(x => x.Owner)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Include(x => x.Nodes).ThenInclude(x => x.Document)
            .ToListAsync();
        var accessibleSpaces = spaces.Where(CanView).ToList();
        return OkData(new
        {
            Spaces = accessibleSpaces
                .Where(x => x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)))
                .Select(ToSpaceItem),
            Nodes = accessibleSpaces
                .SelectMany(x => x.Nodes)
                .Where(x => x.Title.Contains(keyword) || (x.Document != null && x.Document.Content.Contains(keyword)))
                .OrderBy(x => x.Title)
                .Take(100)
                .Select(ToNodeItem)
        });
    }

    [HttpGet("spaces")]
    public async Task<IActionResult> Spaces()
    {
        var spaces = await db.WikiSpaces
            .Include(x => x.Owner)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Include(x => x.Nodes)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
        return OkData(spaces.Where(CanView).Select(ToSpaceItem));
    }

    [HttpPost("spaces")]
    public async Task<IActionResult> CreateSpace(WikiSpaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Fail(2301, "Wiki space name is required.");
        }

        var visibility = NormalizeVisibility(request.Visibility);
        if (visibility is null)
        {
            return Fail(2302, "Visibility value must be Organization or Private.");
        }

        var space = new WikiSpace
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Visibility = visibility,
            OwnerId = CurrentUserId
        };
        db.WikiSpaces.Add(space);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "CreateSpace", space.Id.ToString(), HttpContext);
        return CreatedData(ToSpaceItem((await LoadSpaceAsync(space.Id))!));
    }

    [HttpGet("spaces/{spaceId:guid}")]
    public async Task<IActionResult> SpaceDetail(Guid spaceId)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(space))
        {
            return Fail(2304, "No wiki space permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(new
        {
            Space = ToSpaceItem(space),
            Nodes = space.Nodes.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt).Select(ToNodeItem)
        });
    }

    [HttpPut("spaces/{spaceId:guid}")]
    public async Task<IActionResult> UpdateSpace(Guid spaceId, WikiSpaceRequest request)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(space))
        {
            return Fail(2304, "Only wiki space owner or admin can update the space.", StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Fail(2301, "Wiki space name is required.");
        }

        var visibility = NormalizeVisibility(request.Visibility);
        if (visibility is null)
        {
            return Fail(2302, "Visibility value must be Organization or Private.");
        }

        space.Name = request.Name.Trim();
        space.Description = NormalizeDescription(request.Description);
        space.Visibility = visibility;
        space.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "UpdateSpace", space.Id.ToString(), HttpContext);
        return OkData(ToSpaceItem(space));
    }

    [HttpDelete("spaces/{spaceId:guid}")]
    public async Task<IActionResult> DeleteSpace(Guid spaceId)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(space))
        {
            return Fail(2304, "Only wiki space owner or admin can delete the space.", StatusCodes.Status403Forbidden);
        }

        db.WikiSpaces.Remove(space);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "DeleteSpace", space.Id.ToString(), HttpContext);
        return OkData(new { Id = spaceId });
    }

    [HttpGet("spaces/{spaceId:guid}/members")]
    public async Task<IActionResult> Members(Guid spaceId)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(space))
        {
            return Fail(2304, "No wiki space permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(space.Members.OrderBy(x => x.CreatedAt).Select(ToMemberItem));
    }

    [HttpPut("spaces/{spaceId:guid}/members")]
    public async Task<IActionResult> SetMembers(Guid spaceId, WikiMemberRequest request)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(space))
        {
            return Fail(2304, "Only wiki space owner or admin can manage members.", StatusCodes.Status403Forbidden);
        }

        var permission = NormalizeMemberPermission(request.Permission);
        if (permission is null)
        {
            return Fail(2305, "Permission value must be View, Edit or Admin.");
        }

        var userIds = (request.UserIds ?? Array.Empty<Guid>())
            .Where(x => x != space.OwnerId)
            .Distinct()
            .ToList();
        var users = await db.Users.Where(x => userIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        if (users.Count != userIds.Count)
        {
            return Fail(2306, "Some wiki members do not exist or are disabled.");
        }

        db.WikiSpaceMembers.RemoveRange(space.Members);
        foreach (var user in users)
        {
            db.WikiSpaceMembers.Add(new WikiSpaceMember
            {
                WikiSpaceId = space.Id,
                UserId = user.Id,
                Permission = permission
            });
            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Knowledge base shared with you",
                Content = space.Name,
                Type = "Wiki",
                ResourceType = "WikiSpace",
                ResourceId = space.Id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "SetMembers", space.Id.ToString(), HttpContext);
        return OkData((await LoadSpaceAsync(space.Id))!.Members.Select(ToMemberItem));
    }

    [HttpGet("spaces/{spaceId:guid}/nodes")]
    public async Task<IActionResult> Nodes(Guid spaceId)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(space))
        {
            return Fail(2304, "No wiki space permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(space.Nodes.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt).Select(ToNodeItem));
    }

    [HttpPost("spaces/{spaceId:guid}/nodes")]
    public async Task<IActionResult> CreateNode(Guid spaceId, WikiNodeRequest request)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanEdit(space))
        {
            return Fail(2304, "No wiki edit permission.", StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Fail(2307, "Wiki node title is required.");
        }

        if (request.ParentId.HasValue && !space.Nodes.Any(x => x.Id == request.ParentId.Value))
        {
            return Fail(2308, "Parent node does not belong to this wiki space.");
        }

        if (request.DocumentId.HasValue && !await CanLinkDocumentAsync(request.DocumentId.Value))
        {
            return Fail(2309, "Document does not exist or cannot be linked.");
        }

        var node = new WikiNode
        {
            WikiSpaceId = space.Id,
            ParentId = request.ParentId,
            DocumentId = request.DocumentId,
            Title = request.Title.Trim(),
            SortOrder = request.SortOrder
        };
        db.WikiNodes.Add(node);
        space.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "CreateNode", node.Id.ToString(), HttpContext);
        return CreatedData(ToNodeItem((await LoadNodeAsync(node.Id))!));
    }

    [HttpPut("spaces/{spaceId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> UpdateNode(Guid spaceId, Guid nodeId, WikiNodeRequest request)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanEdit(space))
        {
            return Fail(2304, "No wiki edit permission.", StatusCodes.Status403Forbidden);
        }

        var node = space.Nodes.FirstOrDefault(x => x.Id == nodeId);
        if (node is null)
        {
            return Fail(2310, "Wiki node not found.", StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Fail(2307, "Wiki node title is required.");
        }

        if (request.ParentId == node.Id ||
            request.ParentId.HasValue && !space.Nodes.Any(x => x.Id == request.ParentId.Value))
        {
            return Fail(2308, "Parent node does not belong to this wiki space.");
        }

        if (request.DocumentId.HasValue && !await CanLinkDocumentAsync(request.DocumentId.Value))
        {
            return Fail(2309, "Document does not exist or cannot be linked.");
        }

        node.ParentId = request.ParentId;
        node.DocumentId = request.DocumentId;
        node.Title = request.Title.Trim();
        node.SortOrder = request.SortOrder;
        node.UpdatedAt = DateTime.UtcNow;
        space.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "UpdateNode", node.Id.ToString(), HttpContext);
        return OkData(ToNodeItem((await LoadNodeAsync(node.Id))!));
    }

    [HttpDelete("spaces/{spaceId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> DeleteNode(Guid spaceId, Guid nodeId)
    {
        var space = await LoadSpaceAsync(spaceId);
        if (space is null)
        {
            return Fail(2303, "Wiki space not found.", StatusCodes.Status404NotFound);
        }

        if (!CanEdit(space))
        {
            return Fail(2304, "No wiki edit permission.", StatusCodes.Status403Forbidden);
        }

        var node = space.Nodes.FirstOrDefault(x => x.Id == nodeId);
        if (node is null)
        {
            return Fail(2310, "Wiki node not found.", StatusCodes.Status404NotFound);
        }

        if (space.Nodes.Any(x => x.ParentId == node.Id))
        {
            return Fail(2311, "Delete child nodes before deleting this node.");
        }

        db.WikiNodes.Remove(node);
        space.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Wiki", "DeleteNode", node.Id.ToString(), HttpContext);
        return OkData(new { Id = nodeId });
    }

    private Task<WikiSpace?> LoadSpaceAsync(Guid id)
    {
        return db.WikiSpaces
            .Include(x => x.Owner)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Include(x => x.Nodes).ThenInclude(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private Task<WikiNode?> LoadNodeAsync(Guid id)
    {
        return db.WikiNodes.Include(x => x.Document).FirstOrDefaultAsync(x => x.Id == id);
    }

    private Task<bool> CanLinkDocumentAsync(Guid documentId)
    {
        return db.Documents.AnyAsync(x =>
            x.Id == documentId &&
            (CurrentUserIsAdmin || x.Visibility == "Organization" || x.OwnerId == CurrentUserId ||
             x.Collaborators.Any(member => member.UserId == CurrentUserId)));
    }

    private bool CanView(WikiSpace space)
    {
        return CurrentUserIsAdmin || space.Visibility == "Organization" || space.OwnerId == CurrentUserId ||
               space.Members.Any(x => x.UserId == CurrentUserId);
    }

    private bool CanEdit(WikiSpace space)
    {
        return CurrentUserIsAdmin || space.OwnerId == CurrentUserId ||
               space.Members.Any(x => x.UserId == CurrentUserId && (x.Permission == "Edit" || x.Permission == "Admin"));
    }

    private bool CanManage(WikiSpace space)
    {
        return CurrentUserIsAdmin || space.OwnerId == CurrentUserId ||
               space.Members.Any(x => x.UserId == CurrentUserId && x.Permission == "Admin");
    }

    private static object ToSpaceItem(WikiSpace space)
    {
        return new
        {
            space.Id,
            space.Name,
            space.Description,
            space.Visibility,
            space.OwnerId,
            OwnerName = space.Owner.RealName,
            space.CreatedAt,
            space.UpdatedAt,
            NodeCount = space.Nodes.Count
        };
    }

    private static object ToMemberItem(WikiSpaceMember member)
    {
        return new { member.UserId, UserName = member.User.RealName, member.Permission, member.CreatedAt };
    }

    private static object ToNodeItem(WikiNode node)
    {
        return new
        {
            node.Id,
            node.WikiSpaceId,
            node.ParentId,
            node.DocumentId,
            DocumentTitle = node.Document?.Title,
            node.Title,
            node.SortOrder,
            node.CreatedAt,
            node.UpdatedAt
        };
    }

    private static string? NormalizeVisibility(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "organization" => "Organization",
            "private" => "Private",
            _ => null
        };
    }

    private static string? NormalizeMemberPermission(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "view" => "View",
            "edit" => "Edit",
            "admin" => "Admin",
            _ => null
        };
    }

    private static string? NormalizeDescription(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
