using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/files")]
[Authorize]
public sealed class FilesController(
    AppDbContext db,
    IFileStorageService fileStorageService,
    IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] Guid? folderId)
    {
        var query = AccessibleFilesQuery().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.FileName.Contains(keyword));
        }

        if (folderId.HasValue)
        {
            query = query.Where(x => x.FolderId == folderId.Value);
        }

        var files = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return OkData(files.Select(ToFileItem));
    }

    [HttpGet("trash")]
    public async Task<IActionResult> Trash()
    {
        var files = await db.Files
            .Include(x => x.Uploader)
            .Include(x => x.Folder)
            .Where(x => x.IsDeleted && (CurrentUserIsAdmin || x.UploaderId == CurrentUserId))
            .OrderByDescending(x => x.DeletedAt)
            .ToListAsync();
        return OkData(files.Select(ToFileItem));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
    {
        var file = request.File;
        if (file.Length == 0)
        {
            return Fail(1701, "Uploaded file is empty.");
        }

        if (request.FolderId.HasValue && !await CanUseFolderAsync(request.FolderId.Value))
        {
            return Fail(1705, "Folder not found or no folder permission.", StatusCodes.Status403Forbidden);
        }

        var safeFileName = Path.GetFileName(file.FileName);
        await using var stream = file.OpenReadStream();
        var storageResult = await fileStorageService.SaveAsync(new StorageWriteRequest(
            safeFileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            stream,
            file.Length));

        var storedFile = new StoredFile
        {
            FileName = safeFileName,
            FilePath = storageResult.RelativePath,
            FileSize = storageResult.Size,
            ContentType = storageResult.ContentType,
            UploaderId = CurrentUserId,
            FolderId = request.FolderId
        };

        db.Files.Add(storedFile);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "Upload", storedFile.Id.ToString(), HttpContext);

        return CreatedData(ToFileItem((await LoadFileAsync(storedFile.Id))!));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(file))
        {
            return Fail(1704, "No file permission.", StatusCodes.Status403Forbidden);
        }

        var stream = await fileStorageService.OpenReadAsync(file.FilePath);
        if (stream is null)
        {
            return Fail(1703, "Physical file not found.", StatusCodes.Status404NotFound);
        }

        return File(stream, file.ContentType, file.FileName);
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccess(file))
        {
            return Fail(1704, "No file permission.", StatusCodes.Status403Forbidden);
        }

        var stream = await fileStorageService.OpenReadAsync(file.FilePath);
        if (stream is null)
        {
            return Fail(1703, "Physical file not found.", StatusCodes.Status404NotFound);
        }

        Response.Headers.ContentDisposition = "inline";
        return File(stream, file.ContentType, enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can delete file.", StatusCodes.Status403Forbidden);
        }

        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "MoveToTrash", file.Id.ToString(), HttpContext);
        return OkData(new { file.Id, file.IsDeleted, file.DeletedAt });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || !file.IsDeleted)
        {
            return Fail(1702, "Deleted file not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can restore file.", StatusCodes.Status403Forbidden);
        }

        file.IsDeleted = false;
        file.DeletedAt = null;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "Restore", file.Id.ToString(), HttpContext);
        return OkData(ToFileItem(file));
    }

    [HttpDelete("{id:guid}/permanent")]
    public async Task<IActionResult> PermanentDelete(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || !file.IsDeleted)
        {
            return Fail(1702, "Deleted file not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can permanently delete file.", StatusCodes.Status403Forbidden);
        }

        await fileStorageService.DeleteAsync(file.FilePath);
        db.Files.Remove(file);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "PermanentDelete", id.ToString(), HttpContext);
        return OkData(new { Id = id });
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, MoveFileRequest request)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can move file.", StatusCodes.Status403Forbidden);
        }

        if (request.FolderId.HasValue && !await CanUseFolderAsync(request.FolderId.Value))
        {
            return Fail(1705, "Folder not found or no folder permission.", StatusCodes.Status403Forbidden);
        }

        file.FolderId = request.FolderId;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "Move", file.Id.ToString(), HttpContext);
        return OkData(ToFileItem((await LoadFileAsync(file.Id))!));
    }

    [HttpGet("{id:guid}/shares")]
    public async Task<IActionResult> Shares(Guid id)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can manage file sharing.", StatusCodes.Status403Forbidden);
        }

        return OkData(file.Shares.OrderBy(x => x.CreatedAt).Select(ToShareItem));
    }

    [HttpPut("{id:guid}/shares")]
    public async Task<IActionResult> SetShares(Guid id, FileShareRequest request)
    {
        var file = await LoadFileAsync(id);
        if (file is null || file.IsDeleted)
        {
            return Fail(1702, "File not found.", StatusCodes.Status404NotFound);
        }

        if (!CanManage(file))
        {
            return Fail(1704, "Only uploader or admin can manage file sharing.", StatusCodes.Status403Forbidden);
        }

        var permission = NormalizeSharePermission(request.Permission);
        if (permission is null)
        {
            return Fail(1706, "Share permission must be View or Edit.");
        }

        var userIds = (request.UserIds ?? Array.Empty<Guid>())
            .Where(x => x != file.UploaderId)
            .Distinct()
            .ToList();
        var users = await db.Users.Where(x => userIds.Contains(x.Id) && x.Status == "Active").ToListAsync();
        if (users.Count != userIds.Count)
        {
            return Fail(1707, "Some shared users do not exist or are disabled.");
        }

        db.FileShares.RemoveRange(file.Shares);
        file.Shares.Clear();
        foreach (var user in users)
        {
            var share = new FileShareRecord
            {
                FileId = file.Id,
                UserId = user.Id,
                Permission = permission,
                User = user
            };
            db.FileShares.Add(share);
            file.Shares.Add(share);
            db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "File shared with you",
                Content = file.FileName,
                Type = "File",
                ResourceType = "File",
                ResourceId = file.Id
            });
        }

        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "SetShares", file.Id.ToString(), HttpContext);
        return OkData((await LoadFileAsync(file.Id))!.Shares.Select(ToShareItem));
    }

    [HttpGet("folders")]
    public async Task<IActionResult> Folders([FromQuery] Guid? parentId)
    {
        var query = db.FileFolders.Include(x => x.Owner).Where(x => CurrentUserIsAdmin || x.OwnerId == CurrentUserId);
        if (parentId.HasValue)
        {
            query = query.Where(x => x.ParentId == parentId.Value);
        }

        var folders = await query.OrderBy(x => x.Name).ToListAsync();
        return OkData(folders.Select(ToFolderItem));
    }

    [HttpPost("folders")]
    public async Task<IActionResult> CreateFolder(CreateFolderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Fail(1708, "Folder name is required.");
        }

        if (request.ParentId.HasValue && !await CanUseFolderAsync(request.ParentId.Value))
        {
            return Fail(1705, "Parent folder not found or no folder permission.", StatusCodes.Status403Forbidden);
        }

        var folder = new FileFolder
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            OwnerId = CurrentUserId
        };
        db.FileFolders.Add(folder);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "CreateFolder", folder.Id.ToString(), HttpContext);
        return CreatedData(ToFolderItem((await LoadFolderAsync(folder.Id))!));
    }

    [HttpPut("folders/{id:guid}")]
    public async Task<IActionResult> UpdateFolder(Guid id, UpdateFolderRequest request)
    {
        var folder = await LoadFolderAsync(id);
        if (folder is null || !CanManage(folder))
        {
            return Fail(1705, "Folder not found or no folder permission.", StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Fail(1708, "Folder name is required.");
        }

        if (request.ParentId == folder.Id || request.ParentId.HasValue && !await CanUseFolderAsync(request.ParentId.Value))
        {
            return Fail(1705, "Parent folder not found or no folder permission.", StatusCodes.Status403Forbidden);
        }

        folder.Name = request.Name.Trim();
        folder.ParentId = request.ParentId;
        folder.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "UpdateFolder", folder.Id.ToString(), HttpContext);
        return OkData(ToFolderItem(folder));
    }

    [HttpDelete("folders/{id:guid}")]
    public async Task<IActionResult> DeleteFolder(Guid id)
    {
        var folder = await LoadFolderAsync(id);
        if (folder is null || !CanManage(folder))
        {
            return Fail(1705, "Folder not found or no folder permission.", StatusCodes.Status404NotFound);
        }

        var hasChildren = await db.FileFolders.AnyAsync(x => x.ParentId == id);
        var hasFiles = await db.Files.AnyAsync(x => x.FolderId == id && !x.IsDeleted);
        if (hasChildren || hasFiles)
        {
            return Fail(1709, "Folder must be empty before deletion.");
        }

        db.FileFolders.Remove(folder);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "File", "DeleteFolder", id.ToString(), HttpContext);
        return OkData(new { Id = id });
    }

    private IQueryable<StoredFile> AccessibleFilesQuery()
    {
        return db.Files
            .Include(x => x.Uploader)
            .Include(x => x.Folder)
            .Include(x => x.Shares).ThenInclude(x => x.User)
            .Where(x => CurrentUserIsAdmin || x.UploaderId == CurrentUserId || x.Shares.Any(share => share.UserId == CurrentUserId));
    }

    private Task<StoredFile?> LoadFileAsync(Guid id)
    {
        return db.Files
            .Include(x => x.Uploader)
            .Include(x => x.Folder)
            .Include(x => x.Shares).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private Task<FileFolder?> LoadFolderAsync(Guid id)
    {
        return db.FileFolders.Include(x => x.Owner).FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<bool> CanUseFolderAsync(Guid folderId)
    {
        var folder = await db.FileFolders.FirstOrDefaultAsync(x => x.Id == folderId);
        return folder is not null && (CurrentUserIsAdmin || folder.OwnerId == CurrentUserId);
    }

    private bool CanAccess(StoredFile file)
    {
        return CurrentUserIsAdmin || file.UploaderId == CurrentUserId || file.Shares.Any(x => x.UserId == CurrentUserId);
    }

    private bool CanManage(StoredFile file)
    {
        return CurrentUserIsAdmin || file.UploaderId == CurrentUserId;
    }

    private bool CanManage(FileFolder folder)
    {
        return CurrentUserIsAdmin || folder.OwnerId == CurrentUserId;
    }

    private static object ToFileItem(StoredFile file)
    {
        return new
        {
            file.Id,
            file.FileName,
            file.FileSize,
            file.ContentType,
            file.UploaderId,
            UploaderName = file.Uploader.RealName,
            file.FolderId,
            FolderName = file.Folder?.Name,
            file.IsDeleted,
            file.DeletedAt,
            file.CreatedAt
        };
    }

    private static object ToShareItem(FileShareRecord share)
    {
        return new { share.UserId, UserName = share.User.RealName, share.Permission, share.CreatedAt };
    }

    private static object ToFolderItem(FileFolder folder)
    {
        return new
        {
            folder.Id,
            folder.Name,
            folder.ParentId,
            folder.OwnerId,
            OwnerName = folder.Owner.RealName,
            folder.CreatedAt,
            folder.UpdatedAt
        };
    }

    private static string? NormalizeSharePermission(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "view" => "View",
            "edit" => "Edit",
            _ => null
        };
    }
}
