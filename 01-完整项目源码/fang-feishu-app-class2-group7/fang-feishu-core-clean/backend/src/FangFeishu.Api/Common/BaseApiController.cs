using System.Security.Claims;
using FangFeishu.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace FangFeishu.Api.Common;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : throw new InvalidOperationException("Missing user id.");
        }
    }

    protected bool CurrentUserIsAdmin => User.IsInRole(AppRoles.Admin);

    protected string CurrentClientType =>
        User.FindFirstValue(JwtTokenService.ClientTypeClaim)?.Trim() switch
        {
            { Length: > 0 } clientType => clientType,
            _ => "Web"
        };

    protected IActionResult OkData<T>(T? data, string message = "success")
    {
        return Ok(ApiResponse<T>.Success(data, HttpContext.TraceIdentifier, message));
    }

    protected IActionResult CreatedData<T>(T? data, string message = "created")
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Success(data, HttpContext.TraceIdentifier, message));
    }

    protected IActionResult Fail(int code, string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        return StatusCode(statusCode, ApiResponse<object?>.Fail(code, message, HttpContext.TraceIdentifier));
    }
}
