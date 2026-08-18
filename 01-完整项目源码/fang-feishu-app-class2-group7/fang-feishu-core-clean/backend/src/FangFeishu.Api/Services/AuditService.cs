using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;

namespace FangFeishu.Api.Services;

public interface IAuditService
{
    Task WriteAsync(Guid? userId, string module, string action, string? targetId, HttpContext? httpContext = null);
}

public sealed class AuditService(AppDbContext db) : IAuditService
{
    public async Task WriteAsync(Guid? userId, string module, string action, string? targetId, HttpContext? httpContext = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId = userId,
            Module = module,
            Action = action,
            TargetId = targetId,
            Ip = httpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync();
    }
}

