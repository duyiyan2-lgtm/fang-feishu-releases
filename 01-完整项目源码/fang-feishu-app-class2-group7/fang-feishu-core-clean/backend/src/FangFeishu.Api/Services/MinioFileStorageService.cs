using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Services;

public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public MinioFileStorageService(IOptions<StorageOptions> options)
    {
        var minioOptions = options.Value.Minio;
        var clientBuilder = new MinioClient()
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey);

        if (minioOptions.UseSsl)
        {
            clientBuilder = clientBuilder.WithSSL();
        }

        _client = clientBuilder.Build();
        _bucketName = minioOptions.BucketName;
    }

    public async Task<StorageSaveResult> SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var relativePath = StoragePathHelper.ResolveRelativePath(request.FileName, request.RelativePath);
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(relativePath)
            .WithStreamData(request.Content)
            .WithObjectSize(request.ContentLength)
            .WithContentType(request.ContentType);

        await _client.PutObjectAsync(putObjectArgs, cancellationToken);
        return new StorageSaveResult(relativePath, request.ContentLength, request.ContentType);
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            var normalizedPath = StoragePathHelper.NormalizeRelativePath(relativePath);
            await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(normalizedPath), cancellationToken);

            var stream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(normalizedPath)
                .WithCallbackStream(source => source.CopyTo(stream));

            await _client.GetObjectAsync(getObjectArgs, cancellationToken);
            stream.Position = 0;
            return stream;
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(StoragePathHelper.NormalizeRelativePath(relativePath)), cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
        }
        catch (BucketNotFoundException)
        {
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);
        if (!bucketExists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), cancellationToken);
        }
    }
}
