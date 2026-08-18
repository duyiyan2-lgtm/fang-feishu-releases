using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using FangFeishu.Api.Common;
using FangFeishu.Api.Data;
using FangFeishu.Api.Hubs;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<AgoraOptions>(builder.Configuration.GetSection("Agora"));

builder.Services.AddScoped<NotificationRealtimeInterceptor>();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorCodesToAdd: null);
    });
    options.AddInterceptors(serviceProvider.GetRequiredService<NotificationRealtimeInterceptor>());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FourClientCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var secret = jwtOptions.Secret.Length >= 32 ? jwtOptions.Secret : jwtOptions.Secret.PadRight(32, '#');
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/im"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Missing token id.");
                    return;
                }

                var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var clientType = context.Principal?.FindFirst(JwtTokenService.ClientTypeClaim)?.Value;
                var sessionVersionValue = context.Principal?.FindFirst(JwtTokenService.ClientSessionVersionClaim)?.Value;
                if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(clientType) || !int.TryParse(sessionVersionValue, out var tokenSessionVersion))
                {
                    context.Fail("Missing client session.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var currentSessionVersion = await db.UserClientSessions
                    .Where(x => x.UserId == userId && x.ClientType == clientType)
                    .Select(x => (int?)x.SessionVersion)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                if (currentSessionVersion is null || currentSessionVersion.Value != tokenSessionVersion)
                {
                    context.Fail("This account has signed in on another device of the same client type.");
                    return;
                }

                var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
                if (await tokenRevocationService.IsRevokedAsync(tokenId, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(45);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Keep the wire contract explicit: clients send/receive camelCase JSON while
    // property matching remains case-insensitive for backward compatibility.
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRealtimeEventPublisher, RealtimeEventPublisher>();
builder.Services.AddScoped<ITokenRevocationService, TokenRevocationService>();
builder.Services.AddSingleton<AgoraTokenService>();
builder.Services.AddScoped<LocalFileStorageService>();
builder.Services.AddScoped<MinioFileStorageService>();
builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var storageOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
    return string.Equals(storageOptions.Provider, "Minio", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<MinioFileStorageService>()
        : sp.GetRequiredService<LocalFileStorageService>();
});
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FangFeishu Four-Client Collaboration API",
        Version = "v1",
        Description = "Backend APIs for Web, Desktop, Mobile and WeChat Mini Program clients."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Input: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue("Database:AutoCreate", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.EnsureCreatedAndSeedAsync();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FourClientCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy", Time = DateTime.UtcNow }));
app.MapGet("/health", async (AppDbContext db, HttpContext context, CancellationToken cancellationToken) =>
{
    var databaseHealthy = await db.Database.CanConnectAsync(cancellationToken);
    return databaseHealthy
        ? Results.Ok(new { Status = "Healthy", Database = "Connected", Time = DateTime.UtcNow })
        : Results.Json(
            ApiResponse<object?>.Fail(5001, "Database is unavailable.", context.TraceIdentifier),
            statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapControllers();
app.MapHub<ImHub>("/hubs/im");

app.Run();
