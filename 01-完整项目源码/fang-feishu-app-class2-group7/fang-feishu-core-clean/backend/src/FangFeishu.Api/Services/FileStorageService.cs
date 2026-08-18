namespace FangFeishu.Api.Services;

public sealed record StorageWriteRequest(
    string FileName,
    string ContentType,
    Stream Content,
    long ContentLength,
    string? RelativePath = null);

public sealed record StorageSaveResult(string RelativePath, long Size, string ContentType);

public interface IFileStorageService
{
    Task<StorageSaveResult> SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

internal static class StoragePathHelper
{
    public static string ResolveRelativePath(string fileName, string? requestedPath = null)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            return NormalizeRelativePath(requestedPath);
        }

        var safeFileName = Path.GetFileName(fileName);
        var relativeDir = DateTime.UtcNow.ToString("yyyyMMdd");
        var storedName = $"{Guid.NewGuid():N}_{safeFileName}";
        return $"{relativeDir}/{storedName}";
    }

    public static string NormalizeRelativePath(string relativePath)
    {
        return relativePath
            .Replace('\\', '/')
            .TrimStart('/');
    }
}
