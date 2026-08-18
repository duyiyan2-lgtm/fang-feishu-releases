using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Services;

public interface ITokenRevocationService
{
    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
    Task<RevokedToken> RevokeCurrentTokenAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed class TokenRevocationService(AppDbContext db) : ITokenRevocationService
{
    public Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        return db.RevokedTokens.AnyAsync(x => x.TokenId == tokenId, cancellationToken);
    }

    public async Task<RevokedToken> RevokeCurrentTokenAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var tokenId = user.FindFirstValue(JwtRegisteredClaimNames.Jti)
            ?? throw new InvalidOperationException("Missing token id.");
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new InvalidOperationException("Missing user id.");
        }

        var expValue = user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (!long.TryParse(expValue, out var expUnix))
        {
            throw new InvalidOperationException("Missing token expiration.");
        }

        var existing = await db.RevokedTokens.FirstOrDefaultAsync(x => x.TokenId == tokenId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var revokedToken = new RevokedToken
        {
            UserId = userId,
            TokenId = tokenId,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
        };

        db.RevokedTokens.Add(revokedToken);

        // Keep the revocation table compact in dev/demo environments.
        var expired = db.RevokedTokens.Where(x => x.ExpiresAt <= DateTime.UtcNow);
        db.RevokedTokens.RemoveRange(expired);

        await db.SaveChangesAsync(cancellationToken);
        return revokedToken;
    }
}
