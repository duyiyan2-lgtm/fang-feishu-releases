using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Services;

public sealed class LocalFileStorageService(
    IOptions<StorageOptions> options,
    IWebHostEnvironment environment) : IFileStorageService
{
    private readonly StorageOptions _options = options.Value;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<StorageSaveResult> SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken = default)
    {
        var relativePath = StoragePathHelper.ResolveRelativePath(request.FileName, request.RelativePath);
        var physicalPath = GetPhysicalPath(relativePath);
        var dir = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var targetStream = File.Create(physicalPath);
        await request.Content.CopyToAsync(targetStream, cancellationToken);

        return new StorageSaveResult(relativePath, request.ContentLength, request.ContentType);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        Stream? stream = File.Exists(physicalPath)
            ? File.OpenRead(physicalPath)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    private string GetPhysicalPath(string relativePath)
    {
        var configuredRoot = _options.RootPath;
        var root = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(_environment.ContentRootPath, configuredRoot);
        Directory.CreateDirectory(root);

        return Path.Combine(root, StoragePathHelper.NormalizeRelativePath(relativePath).Replace('/', Path.DirectorySeparatorChar));
    }
}
