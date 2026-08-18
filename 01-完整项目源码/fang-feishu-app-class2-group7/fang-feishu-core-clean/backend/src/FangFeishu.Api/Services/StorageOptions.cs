namespace FangFeishu.Api.Services;

public sealed class StorageOptions
{
    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "storage";
    public MinioStorageOptions Minio { get; set; } = new();
}

public sealed class MinioStorageOptions
{
    public string Endpoint { get; set; } = "127.0.0.1:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "fang-feishu";
    public bool UseSsl { get; set; }
}
