namespace FangFeishu.Api.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "FangFeishu";
    public string Audience { get; set; } = "FangFeishuClients";
    public string Secret { get; set; } = "dev-only-secret-key-change-me-32chars";
    public int ExpireMinutes { get; set; } = 720;
}

