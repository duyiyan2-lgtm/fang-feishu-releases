using FangFeishu.Api.Services;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class AgoraTokenServiceTests
{
    [Fact]
    public void CreateJoinToken_ShouldReportUnconfigured_WhenAppIdIsMissing()
    {
        var service = CreateService(new AgoraOptions());

        var token = service.CreateJoinToken(Guid.NewGuid(), "ff_test_room");

        Assert.False(token.Configured);
        Assert.Equal(string.Empty, token.AppId);
        Assert.Null(token.RtcToken);
    }

    [Fact]
    public void CreateJoinToken_ShouldAllowAppIdOnlyMode_WhenCertificateIsMissing()
    {
        var service = CreateService(new AgoraOptions
        {
            AppId = "0123456789abcdef0123456789abcdef"
        });

        var token = service.CreateJoinToken(Guid.NewGuid(), "ff_test_room");

        Assert.True(token.Configured);
        Assert.False(token.TokenRequired);
        Assert.Equal("0123456789abcdef0123456789abcdef", token.AppId);
        Assert.Null(token.RtcToken);
    }

    [Fact]
    public void CreateJoinToken_ShouldBuildRtcToken_WhenCertificateIsConfigured()
    {
        var service = CreateService(new AgoraOptions
        {
            AppId = "0123456789abcdef0123456789abcdef",
            AppCertificate = "fedcba9876543210fedcba9876543210",
            TokenExpireSeconds = 600
        });

        var token = service.CreateJoinToken(Guid.Parse("11111111-1111-1111-1111-111111111111"), "ff_test_room");

        Assert.True(token.Configured);
        Assert.True(token.TokenRequired);
        Assert.NotEqual(0u, token.Uid);
        Assert.False(string.IsNullOrWhiteSpace(token.RtcToken));
        Assert.NotNull(token.TokenExpireAt);
    }

    [Fact]
    public void CreateJoinToken_ShouldUseDifferentUids_ForSameUserOnDifferentClients()
    {
        var service = CreateService(new AgoraOptions());
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var desktop = service.CreateJoinToken(userId, "ff_test_room", "Desktop");
        var android = service.CreateJoinToken(userId, "ff_test_room", "Android");

        Assert.NotEqual(desktop.Uid, android.Uid);
    }

    [Fact]
    public void GetUid_ShouldMatchTheUidUsedInJoinToken()
    {
        var service = CreateService(new AgoraOptions());
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var token = service.CreateJoinToken(userId, "ff_test_room", "Android");

        Assert.Equal(token.Uid, service.GetUid(userId, "Android"));
    }

    [Fact]
    public void GetUid_ShouldKeepStableLegacyAndAndroidVectors_ForRollingDeployments()
    {
        var service = CreateService(new AgoraOptions());
        var userId = Guid.Parse("0bb2d41b-2396-4d81-87d9-c411ee5fc57d");

        Assert.Equal(1630889624u, service.GetUid(userId));
        Assert.Equal(1003467861u, service.GetUid(userId, "Android"));
    }

    private static AgoraTokenService CreateService(AgoraOptions options)
    {
        return new AgoraTokenService(Options.Create(options));
    }
}
