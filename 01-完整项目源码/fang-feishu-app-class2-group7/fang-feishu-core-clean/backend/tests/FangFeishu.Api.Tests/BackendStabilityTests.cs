using System.Text.Json;
using FangFeishu.Api.Common;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FangFeishu.Api.Tests;

public sealed class BackendStabilityTests
{
    [Fact]
    public async Task UnhandledException_ShouldReturnStructuredJsonWithTraceId()
    {
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("test failure"),
            NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-test-500";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(5000, json.RootElement.GetProperty("code").GetInt32());
        Assert.Equal("trace-test-500", json.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task RealtimeFailureAfterCommit_ShouldNotFailDatabaseSave()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new NotificationRealtimeInterceptor(new ThrowingRealtimePublisher()))
            .Options;
        await using var db = new AppDbContext(options);
        var user = new User
        {
            Username = "stable_user",
            RealName = "Stable User",
            PasswordHash = "hash",
            Status = "Active"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Stored notification",
            Content = "Realtime delivery may fail.",
            Type = "System"
        });

        var affected = await db.SaveChangesAsync();

        Assert.Equal(1, affected);
        Assert.Equal(1, await db.Notifications.CountAsync());
    }

    private sealed class ThrowingRealtimePublisher : IRealtimeEventPublisher
    {
        public Task SendToUserAsync(
            Guid userId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("SignalR unavailable"));

        public Task SendToUsersAsync(
            IEnumerable<Guid> userIds,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("SignalR unavailable"));
    }
}
