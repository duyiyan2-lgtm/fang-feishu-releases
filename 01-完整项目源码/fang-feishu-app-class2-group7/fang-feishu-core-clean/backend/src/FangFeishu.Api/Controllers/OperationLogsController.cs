using FangFeishu.Api.Common;
using FangFeishu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/operation-logs")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class OperationLogsController(AppDbContext db) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? module, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.OperationLogs.Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module == module);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return OkData(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new
            {
                x.Id,
                x.UserId,
                UserName = x.User == null ? null : x.User.RealName,
                x.Module,
                x.Action,
                x.TargetId,
                x.Ip,
                x.CreatedAt
            })
        });
    }
}

