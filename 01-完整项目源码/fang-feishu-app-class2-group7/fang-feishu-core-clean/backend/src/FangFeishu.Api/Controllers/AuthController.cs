using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(
    AppDbContext db,
    PasswordHasher passwordHasher,
    JwtTokenService jwtTokenService,
    IAuditService auditService,
    ITokenRevocationService tokenRevocationService) : BaseApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == request.Username);

        if (user is null || user.Status != "Active" || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Fail(1001, "Invalid username or password.", StatusCodes.Status401Unauthorized);
        }

        var roles = user.UserRoles.Select(x => x.Role.RoleCode).ToList();
        var clientType = NormalizeClientType(request.ClientType ?? Request.Headers["X-Client-Type"].FirstOrDefault());
        var clientSessionVersion = await StartClientSessionAsync(user.Id, clientType);
        var token = jwtTokenService.CreateToken(user, roles, clientType, clientSessionVersion);
        await auditService.WriteAsync(user.Id, "Auth", "Login", user.Id.ToString(), HttpContext);

        return OkData(new LoginResponse(token.Token, token.ExpiresAt, ToCurrentUser(user, roles)));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var realName = request.RealName?.Trim() ?? string.Empty;

        if (username.Length is < 2 or > 64 || !username.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
        {
            return Fail(1003, "Username must be 2-64 characters and contain only letters, digits, underscores, or hyphens.");
        }

        if (password.Length < 6)
        {
            return Fail(1004, "Password must be at least 6 characters.");
        }

        if (realName.Length is < 1 or > 64)
        {
            return Fail(1005, "Real name must be 1-64 characters.");
        }

        if (await db.Users.AnyAsync(x => x.Username == username))
        {
            return Fail(1006, "Username already exists.", StatusCodes.Status409Conflict);
        }

        var userRole = await db.Roles.SingleOrDefaultAsync(x => x.RoleCode == AppRoles.User);
        if (userRole is null)
        {
            return Fail(1007, "Default user role is not configured.", StatusCodes.Status503ServiceUnavailable);
        }

        var user = new Domain.User
        {
            Username = username,
            PasswordHash = passwordHasher.Hash(password),
            RealName = realName,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Profile = new Domain.EmployeeProfile()
        };
        user.UserRoles.Add(new Domain.UserRole { User = user, Role = userRole });

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var roles = new[] { AppRoles.User };
        var clientType = NormalizeClientType(request.ClientType ?? Request.Headers["X-Client-Type"].FirstOrDefault());
        var clientSessionVersion = await StartClientSessionAsync(user.Id, clientType);
        var token = jwtTokenService.CreateToken(user, roles, clientType, clientSessionVersion);
        await auditService.WriteAsync(user.Id, "Auth", "Register", user.Id.ToString(), HttpContext);

        return CreatedData(new LoginResponse(token.Token, token.ExpiresAt, ToCurrentUser(user, roles)), "registered");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == CurrentUserId);

        if (user is null)
        {
            return Fail(1002, "User not found.", StatusCodes.Status404NotFound);
        }

        return OkData(ToCurrentUser(user, user.UserRoles.Select(x => x.Role.RoleCode).ToList()));
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateCurrentUserProfileRequest request)
    {
        var realName = request.RealName?.Trim();
        var email = request.Email?.Trim();
        var phone = request.Phone?.Trim();
        var position = request.Position?.Trim();
        var avatarUrl = request.AvatarUrl?.Trim();
        var workPlace = request.WorkPlace?.Trim();
        var bio = request.Bio?.Trim();

        if (realName is not null && realName.Length is < 1 or > 64)
        {
            return Fail(1008, "Real name must be 1-64 characters.");
        }

        if (email?.Length > 256 || phone?.Length > 64 || position?.Length > 160 || avatarUrl?.Length > 500 || workPlace?.Length > 160 || bio?.Length > 1000)
        {
            return Fail(1009, "One or more profile fields are too long.");
        }

        var user = await db.Users
            .Include(x => x.Department)
            .Include(x => x.Profile)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == CurrentUserId);

        if (user is null)
        {
            return Fail(1002, "User not found.", StatusCodes.Status404NotFound);
        }

        if (realName is not null) user.RealName = realName;
        if (email is not null) user.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        if (phone is not null) user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;

        user.Profile ??= new Domain.EmployeeProfile { UserId = user.Id };
        if (position is not null) user.Profile.Position = string.IsNullOrWhiteSpace(position) ? null : position;
        if (avatarUrl is not null) user.Profile.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        if (workPlace is not null) user.Profile.WorkPlace = string.IsNullOrWhiteSpace(workPlace) ? null : workPlace;
        if (bio is not null) user.Profile.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(user.Id, "Auth", "UpdateProfile", user.Id.ToString(), HttpContext);
        return OkData(ToCurrentUser(user, user.UserRoles.Select(x => x.Role.RoleCode).ToList()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var revokedToken = await tokenRevocationService.RevokeCurrentTokenAsync(User, cancellationToken);
        var clientType = User.FindFirst(JwtTokenService.ClientTypeClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(clientType))
        {
            var session = await db.UserClientSessions.FirstOrDefaultAsync(
                x => x.UserId == CurrentUserId && x.ClientType == clientType,
                cancellationToken);
            if (session is not null)
            {
                session.SessionVersion++;
                session.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        await auditService.WriteAsync(CurrentUserId, "Auth", "Logout", revokedToken.TokenId, HttpContext);
        return OkData(new LogoutResponse(revokedToken.TokenId, revokedToken.ExpiresAt));
    }

    private static CurrentUserResponse ToCurrentUser(Domain.User user, IReadOnlyList<string> roles)
    {
        return new CurrentUserResponse(
            user.Id,
            user.Username,
            user.RealName,
            user.Email,
            user.Phone,
            user.DepartmentId,
            user.Department?.Name,
            roles,
            user.Profile?.Position,
            user.Profile?.AvatarUrl,
            user.Profile?.WorkPlace,
            user.Profile?.Bio);
    }

    private async Task<int> StartClientSessionAsync(Guid userId, string clientType)
    {
        var session = await db.UserClientSessions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ClientType == clientType);
        if (session is null)
        {
            session = new Domain.UserClientSession
            {
                UserId = userId,
                ClientType = clientType,
                SessionVersion = 1
            };
            db.UserClientSessions.Add(session);
        }
        else
        {
            session.SessionVersion++;
            session.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return session.SessionVersion;
    }

    private static string NormalizeClientType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "android" => "Android",
            "desktop" => "Desktop",
            "miniprogram" or "mini-program" or "mini_program" => "MiniProgram",
            _ => "Web"
        };
    }
}
