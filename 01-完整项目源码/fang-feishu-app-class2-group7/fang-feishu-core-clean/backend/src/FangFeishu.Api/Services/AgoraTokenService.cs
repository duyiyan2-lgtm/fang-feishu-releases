using System.Security.Cryptography;
using System.Text;
using AgoraIO.Media;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Services;

public sealed class AgoraOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppCertificate { get; set; } = string.Empty;
    public uint TokenExpireSeconds { get; set; } = 3600;
}

public sealed record AgoraJoinToken(
    string AppId,
    string? RtcToken,
    uint Uid,
    DateTimeOffset? TokenExpireAt,
    bool TokenRequired,
    bool Configured);

public sealed class AgoraTokenService(IOptions<AgoraOptions> options)
{
    private readonly AgoraOptions options = options.Value;

    public AgoraJoinToken CreateJoinToken(Guid userId, string channelName, string? clientType = null)
    {
        var uid = GetUid(userId, clientType);
        var tokenExpireSeconds = options.TokenExpireSeconds == 0 ? 3600 : options.TokenExpireSeconds;
        var hasAppId = !string.IsNullOrWhiteSpace(options.AppId);
        var hasCertificate = !string.IsNullOrWhiteSpace(options.AppCertificate);

        if (!hasAppId)
        {
            return new AgoraJoinToken(string.Empty, null, uid, null, hasCertificate, false);
        }

        if (!hasCertificate)
        {
            return new AgoraJoinToken(options.AppId, null, uid, null, false, true);
        }

        var token = RtcTokenBuilder2.buildTokenWithUid(
            options.AppId,
            options.AppCertificate,
            channelName,
            uid,
            RtcTokenBuilder2.Role.RolePublisher,
            tokenExpireSeconds,
            tokenExpireSeconds);

        return new AgoraJoinToken(
            options.AppId,
            token,
            uid,
            DateTimeOffset.UtcNow.AddSeconds(tokenExpireSeconds),
            true,
            true);
    }

    public uint GetUid(Guid userId, string? clientType = null) => ToAgoraUid(userId, clientType);

    private static uint ToAgoraUid(Guid userId, string? clientType)
    {
        // Agora requires a distinct UID for every concurrent connection in one channel.
        // A user may join from PC and Android at the same time, so the client type is
        // included in the UID material while preserving the legacy UID for callers
        // that do not have a client type yet.
        var identity = string.IsNullOrWhiteSpace(clientType)
            ? userId.ToString("N")
            : $"{userId:N}:{clientType.Trim().ToUpperInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        // The Android SDK exposes Agora UID through Java's signed Int. Keep the
        // generated UID positive on every supported client while remaining stable.
        var value = BitConverter.ToUInt32(bytes, 0) & int.MaxValue;
        return value == 0 ? 1 : value;
    }
}
