using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FangFeishu.Api.Data;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace FangFeishu.Api.Tests;

public sealed class StorageAndAuthTests
{
    [Fact]
    public async Task TokenRevocationService_ShouldStoreRevokedToken_AndPreventDuplicates()
    {
        await using var db = CreateDbContext();
        var service = new TokenRevocationService(db);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(userId, "token-1", expiresAt);

        var first = await service.RevokeCurrentTokenAsync(principal);
        var second = await service.RevokeCurrentTokenAsync(principal);

        Assert.Equal("token-1", first.TokenId);
        Assert.Equal(first.Id, second.Id);
        Assert.True(await service.IsRevokedAsync("token-1"));
        Assert.Equal(1, await db.RevokedTokens.CountAsync());
    }

    [Fact]
    public async Task TokenRevocationService_ShouldCleanupExpiredTokens_WhenRevokingNewOne()
    {
        await using var db = CreateDbContext();
        db.RevokedTokens.Add(new FangFeishu.Api.Domain.RevokedToken
        {
            UserId = Guid.NewGuid(),
            TokenId = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var service = new TokenRevocationService(db);
        var principal = CreatePrincipal(Guid.NewGuid(), "token-2", DateTimeOffset.UtcNow.AddHours(1));
        await service.RevokeCurrentTokenAsync(principal);

        Assert.False(await service.IsRevokedAsync("expired-token"));
        Assert.True(await service.IsRevokedAsync("token-2"));
    }

    [Fact]
    public async Task LocalFileStorageService_ShouldSaveReadAndDeleteFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "fang-feishu-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new StorageOptions
        {
            Provider = "Local",
            RootPath = "storage-test"
        });
        var environment = new FakeWebHostEnvironment(tempRoot);
        var service = new LocalFileStorageService(options, environment);

        var bytes = System.Text.Encoding.UTF8.GetBytes("hello fang feishu");
        await using (var uploadStream = new MemoryStream(bytes, writable: false))
        {
            var saveResult = await service.SaveAsync(new StorageWriteRequest(
                "hello.txt",
                "text/plain",
                uploadStream,
                bytes.Length,
                "unit/hello.txt"));

            Assert.Equal("unit/hello.txt", saveResult.RelativePath);
        }

        await using (var downloadStream = await service.OpenReadAsync("unit/hello.txt"))
        {
            Assert.NotNull(downloadStream);
            using var reader = new StreamReader(downloadStream!);
            Assert.Equal("hello fang feishu", await reader.ReadToEndAsync());
        }

        await service.DeleteAsync("unit/hello.txt");
        Assert.Null(await service.OpenReadAsync("unit/hello.txt"));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string tokenId, DateTimeOffset expiresAt)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToUnixTimeSeconds().ToString())
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FangFeishu.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(contentRootPath, "wwwroot");
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
