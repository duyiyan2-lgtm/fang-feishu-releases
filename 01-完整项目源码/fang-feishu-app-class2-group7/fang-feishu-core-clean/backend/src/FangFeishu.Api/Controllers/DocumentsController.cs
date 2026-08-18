using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] bool includeDeleted = false)
    {
        IQueryable<Document> source = includeDeleted ? db.Documents.IgnoreQueryFilters() : db.Documents;
        var query = source
            .Include(x => x.Owner)
            .Include(x => x.Collaborators)
            .AsQueryable();
        if (!CurrentUserIsAdmin)
        {
            query = includeDeleted
                ? query.Where(x =>
                    (!x.IsDeleted && (x.Visibility == "Organization" ||
                        x.OwnerId == CurrentUserId ||
                        x.Collaborators.Any(member => member.UserId == CurrentUserId))) ||
                    (x.IsDeleted && x.OwnerId == CurrentUserId))
                : query.Where(x =>
                    x.Visibility == "Organization" ||
                    x.OwnerId == CurrentUserId ||
                    x.Collaborators.Any(member => member.UserId == CurrentUserId));
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Title.Contains(keyword));
        }

        var docs = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return OkData(docs.Select(x => new
        {
            x.Id,
            x.Title,
            x.OwnerId,
            OwnerName = x.Owner.RealName,
            x.Visibility,
            x.IsDeleted,
            x.DeletedAt,
            x.DeletedBy,
            CanEdit = CurrentUserIsAdmin || x.OwnerId == CurrentUserId ||
                x.Collaborators.Any(member => member.UserId == CurrentUserId && member.Permission == "Edit"),
            x.CreatedAt,
            x.UpdatedAt
        }));
    }

    [HttpGet("trash")]
    public async Task<IActionResult> Trash([FromQuery] string? keyword)
    {
        var query = db.Documents
            .IgnoreQueryFilters()
            .Include(x => x.Owner)
            .Where(x => x.IsDeleted);
        if (!CurrentUserIsAdmin)
        {
            query = query.Where(x => x.OwnerId == CurrentUserId);
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.Title.Contains(value));
        }

        var docs = await query.OrderByDescending(x => x.DeletedAt).ToListAsync();
        return OkData(docs.Select(x => new
        {
            x.Id,
            x.Title,
            x.OwnerId,
            OwnerName = x.Owner.RealName,
            x.Visibility,
            x.IsDeleted,
            x.DeletedAt,
            x.DeletedBy,
            x.CreatedAt,
            x.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(DocumentRequest request)
    {
        var doc = new Document
        {
            Title = request.Title,
            Content = request.Content ?? string.Empty,
            OwnerId = CurrentUserId,
            UpdatedBy = CurrentUserId
        };
        doc.Versions.Add(new DocumentVersion { Document = doc, CreatedBy = CurrentUserId, ContentSnapshot = doc.Content });

        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "Create", doc.Id.ToString(), HttpContext);
        return CreatedData(new { doc.Id, doc.Title, doc.Content, doc.CreatedAt, doc.UpdatedAt });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var doc = await db.Documents
            .Include(x => x.Owner)
            .Include(x => x.Comments).ThenInclude(x => x.User)
            .Include(x => x.Versions)
            .Include(x => x.Collaborators).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (doc is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(doc))
        {
            return Fail(1605, "No document permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(ToDocumentDetail(doc));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, DocumentRequest request)
    {
        var doc = await db.Documents
            .Include(x => x.Collaborators)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (doc is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanEdit(doc))
        {
            return Fail(1605, "No document edit permission.", StatusCodes.Status403Forbidden);
        }

        doc.Title = request.Title;
        doc.Content = request.Content ?? string.Empty;
        doc.UpdatedBy = CurrentUserId;
        doc.UpdatedAt = DateTime.UtcNow;
        db.DocumentVersions.Add(new DocumentVersion { DocumentId = doc.Id, CreatedBy = CurrentUserId, ContentSnapshot = doc.Content });

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "Update", doc.Id.ToString(), HttpContext);
        return OkData(new { doc.Id, doc.Title, doc.Content, doc.UpdatedAt });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> Comment(Guid id, DocumentCommentRequest request)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(document))
        {
            return Fail(1605, "No document permission.", StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Fail(1602, "Comment content is required.");
        }

        var comment = new DocumentComment { DocumentId = id, UserId = CurrentUserId, Content = request.Content };
        db.DocumentComments.Add(comment);
        if (document.OwnerId != CurrentUserId)
        {
            db.Notifications.Add(new Notification
            {
                UserId = document.OwnerId,
                Title = "New document comment",
                Content = request.Content.Length > 80 ? request.Content[..80] : request.Content,
                Type = "Document",
                ResourceType = "Document",
                ResourceId = id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "Comment", id.ToString(), HttpContext);
        return CreatedData(new { comment.Id, comment.DocumentId, comment.UserId, comment.Content, comment.CreatedAt });
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> Comments(Guid id)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(document))
        {
            return Fail(1605, "No document permission.", StatusCodes.Status403Forbidden);
        }

        var comments = await db.DocumentComments
            .Include(x => x.User)
            .Where(x => x.DocumentId == id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        return OkData(comments.Select(ToCommentItem));
    }

    [HttpDelete("{documentId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid documentId, Guid commentId)
    {
        var comment = await db.DocumentComments
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == commentId && x.DocumentId == documentId);
        if (comment is null)
        {
            return Fail(1603, "Comment not found.", StatusCodes.Status404NotFound);
        }

        if (comment.UserId != CurrentUserId && comment.Document.OwnerId != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(1604, "No permission to delete comment.", StatusCodes.Status403Forbidden);
        }

        db.DocumentComments.Remove(comment);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "DeleteComment", commentId.ToString(), HttpContext);
        return OkData(new { Id = commentId });
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> Versions(Guid id)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(document))
        {
            return Fail(1605, "No document permission.", StatusCodes.Status403Forbidden);
        }

        var versions = await db.DocumentVersions
            .Where(x => x.DocumentId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();

        return OkData(versions.Select(x => new { x.Id, x.DocumentId, x.CreatedBy, x.CreatedAt, x.ContentSnapshot }));
    }

    [HttpPut("{id:guid}/collaborators")]
    public async Task<IActionResult> SetCollaborators(Guid id, DocumentCollaboratorRequest request)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(document))
        {
            return Fail(1605, "Only document owner or admin can manage collaborators.", StatusCodes.Status403Forbidden);
        }

        var permission = NormalizeCollaboratorPermission(request.Permission);
        if (permission is null)
        {
            return Fail(1606, "Permission value must be View or Edit.");
        }

        var userIds = (request.UserIds ?? Array.Empty<Guid>())
            .Where(x => x != document.OwnerId)
            .Distinct()
            .ToList();
        var users = await db.Users.Where(x => userIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        if (users.Count != userIds.Count)
        {
            return Fail(1607, "Some collaborators do not exist or are disabled.");
        }

        db.DocumentCollaborators.RemoveRange(document.Collaborators);
        foreach (var user in users)
        {
            db.DocumentCollaborators.Add(new DocumentCollaborator
            {
                DocumentId = document.Id,
                UserId = user.Id,
                Permission = permission
            });

            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Document shared with you",
                Content = document.Title,
                Type = "Document",
                ResourceType = "Document",
                ResourceId = document.Id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "SetCollaborators", document.Id.ToString(), HttpContext);
        return OkData(await ToCollaboratorsAsync(document.Id));
    }

    [HttpGet("{id:guid}/collaborators")]
    public async Task<IActionResult> Collaborators(Guid id)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanView(document))
        {
            return Fail(1605, "No document permission.", StatusCodes.Status403Forbidden);
        }

        return OkData(document.Collaborators.Select(ToCollaboratorItem));
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<IActionResult> UpdateVisibility(Guid id, UpdateDocumentVisibilityRequest request)
    {
        var document = await db.Documents.FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(document))
        {
            return Fail(1605, "Only document owner or admin can update visibility.", StatusCodes.Status403Forbidden);
        }

        var visibility = NormalizeVisibility(request.Visibility);
        if (visibility is null)
        {
            return Fail(1608, "Visibility value must be Organization or Private.");
        }

        document.Visibility = visibility;
        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "UpdateVisibility", document.Id.ToString(), HttpContext);
        return OkData(new { document.Id, document.Visibility });
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    public async Task<IActionResult> RestoreVersion(Guid id, Guid versionId)
    {
        var document = await db.Documents
            .Include(x => x.Collaborators)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanEdit(document))
        {
            return Fail(1605, "No document edit permission.", StatusCodes.Status403Forbidden);
        }

        var version = await db.DocumentVersions.FirstOrDefaultAsync(x => x.Id == versionId && x.DocumentId == id);
        if (version is null)
        {
            return Fail(1609, "Document version not found.", StatusCodes.Status404NotFound);
        }

        document.Content = version.ContentSnapshot;
        document.UpdatedBy = CurrentUserId;
        document.UpdatedAt = DateTime.UtcNow;
        db.DocumentVersions.Add(new DocumentVersion
        {
            DocumentId = document.Id,
            CreatedBy = CurrentUserId,
            ContentSnapshot = document.Content
        });
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "RestoreVersion", document.Id.ToString(), HttpContext);
        return OkData(new { document.Id, document.Content, document.UpdatedAt });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var document = await db.Documents.FirstOrDefaultAsync(x => x.Id == id);
        if (document is null)
        {
            return Fail(1601, "Document not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(document))
        {
            return Fail(1605, "Only document owner or admin can delete document.", StatusCodes.Status403Forbidden);
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = CurrentUserId;
        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "MoveToTrash", id.ToString(), HttpContext);
        return OkData(new { Id = id, document.IsDeleted, document.DeletedAt });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var document = await db.Documents.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);
        if (document is null)
        {
            return Fail(1601, "Deleted document not found.", StatusCodes.Status404NotFound);
        }

        if (document.OwnerId != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(1605, "Only document owner or admin can restore document.", StatusCodes.Status403Forbidden);
        }

        document.IsDeleted = false;
        document.DeletedAt = null;
        document.DeletedBy = null;
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = CurrentUserId;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "Restore", id.ToString(), HttpContext);
        return OkData(new { document.Id, document.IsDeleted, document.UpdatedAt });
    }

    [HttpDelete("{id:guid}/permanent")]
    public async Task<IActionResult> PermanentDelete(Guid id)
    {
        var document = await db.Documents.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);
        if (document is null)
        {
            return Fail(1601, "Deleted document not found.", StatusCodes.Status404NotFound);
        }

        if (document.OwnerId != CurrentUserId && !CurrentUserIsAdmin)
        {
            return Fail(1605, "Only document owner or admin can permanently delete document.", StatusCodes.Status403Forbidden);
        }

        db.Documents.Remove(document);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Document", "PermanentDelete", id.ToString(), HttpContext);
        return OkData(new { Id = id, PermanentlyDeleted = true });
    }

    private static object ToDocumentDetail(Document doc)
    {
        return new
        {
            doc.Id,
            doc.Title,
            doc.Content,
            doc.OwnerId,
            OwnerName = doc.Owner.RealName,
            doc.Visibility,
            doc.IsDeleted,
            doc.DeletedAt,
            doc.DeletedBy,
            doc.UpdatedBy,
            doc.CreatedAt,
            doc.UpdatedAt,
            Comments = doc.Comments.OrderBy(x => x.CreatedAt).Select(ToCommentItem),
            Versions = doc.Versions.OrderByDescending(x => x.CreatedAt).Take(5).Select(x => new
            {
                x.Id,
                x.CreatedBy,
                x.CreatedAt
            })
        };
    }

    private static object ToCommentItem(DocumentComment comment)
    {
        return new
        {
            comment.Id,
            comment.DocumentId,
            comment.UserId,
            UserName = comment.User.RealName,
            comment.Content,
            comment.CreatedAt
        };
    }

    private bool CanView(Document document)
    {
        return CurrentUserIsAdmin ||
               document.Visibility == "Organization" ||
               document.OwnerId == CurrentUserId ||
               document.Collaborators.Any(x => x.UserId == CurrentUserId);
    }

    private bool CanEdit(Document document)
    {
        return CurrentUserIsAdmin ||
               document.OwnerId == CurrentUserId ||
               document.Collaborators.Any(x => x.UserId == CurrentUserId && x.Permission == "Edit");
    }

    private bool CanManage(Document document)
    {
        return CurrentUserIsAdmin || document.OwnerId == CurrentUserId;
    }

    private async Task<IEnumerable<object>> ToCollaboratorsAsync(Guid documentId)
    {
        var collaborators = await db.DocumentCollaborators
            .Include(x => x.User)
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        return collaborators.Select(ToCollaboratorItem);
    }

    private static object ToCollaboratorItem(DocumentCollaborator collaborator)
    {
        return new
        {
            collaborator.UserId,
            UserName = collaborator.User.RealName,
            collaborator.Permission,
            collaborator.CreatedAt
        };
    }

    private static string? NormalizeCollaboratorPermission(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "view" => "View",
            "edit" => "Edit",
            _ => null
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
}
