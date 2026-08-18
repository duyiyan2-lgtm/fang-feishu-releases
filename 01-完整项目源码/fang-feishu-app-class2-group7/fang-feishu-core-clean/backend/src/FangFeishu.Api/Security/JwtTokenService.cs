using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FangFeishu.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FangFeishu.Api.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public const string ClientTypeClaim = "client_type";
    public const string ClientSessionVersionClaim = "client_session_version";
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateToken(User user, IEnumerable<string> roles, string clientType, int clientSessionVersion)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);
        var tokenId = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("real_name", user.RealName),
            new(ClientTypeClaim, clientType),
            new(ClientSessionVersionClaim, clientSessionVersion.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var secret = _options.Secret.Length >= 32 ? _options.Secret : _options.Secret.PadRight(32, '#');
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
