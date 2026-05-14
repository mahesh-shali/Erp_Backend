using System.Text;
using Erp.Api.Auth;
using Erp.Api.Caching;
using Erp.Api.Configuration;
using Erp.Api.Data;
using Erp.Api.Grpc;
using Erp.Api.Models;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var contentRoot = Directory.GetCurrentDirectory();
EnvLoader.Load(
    Path.Combine(contentRoot, ".env"),
    Path.Combine(contentRoot, ".env.development"));

var builder = WebApplication.CreateBuilder(args);
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPort, out var port))
{
    var renderUrl = $"http://0.0.0.0:{port}";
    builder.Configuration["Kestrel:Endpoints:Https:Url"] = renderUrl;
    builder.Configuration["Kestrel:Endpoints:Https:Protocols"] = "Http1";
    builder.WebHost.UseUrls(renderUrl);
}
else if (builder.Environment.IsDevelopment())
{
    var developmentPort = GetAvailablePort(5250);
    if (developmentPort != 5250)
    {
        Console.WriteLine($"Development port 5250 is in use. Starting ERP API on https://localhost:{developmentPort} instead.");
    }

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(developmentPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
            listenOptions.UseHttps();
        });
    });
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("Google"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var redisConfiguration = NormalizeRedisConfiguration(
    builder.Configuration["Redis:Configuration"] ??
    builder.Configuration["Redis:Url"] ??
    builder.Configuration["REDIS_URL"]);
if (!string.IsNullOrWhiteSpace(redisConfiguration))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfiguration;
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "erp:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<CacheService>();
builder.Services.AddScoped<PasswordHasher<AppUser>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue("Database:AutoMigrate", true))
{
    try
    {
        var migrationResult = await DatabaseMigrator.MigrateLatestAsync(app.Services, app.Logger);
        var seedState = $"{migrationResult.LatestMigration ?? "no-migrations"}:{DatabaseSeeder.SeedFingerprint}";
        var previousSeedState = await StartupStateStore.GetValueAsync(app.Services, "database-seed");

        if (previousSeedState == seedState)
        {
            app.Logger.LogInformation("Database seed is already current. Skipping startup seed checks.");
        }
        else
        {
            app.Logger.LogInformation("Database seed changed or has not run yet. Applying startup seed data.");
            await DatabaseSeeder.SeedAsync(app.Services);
            await StartupStateStore.SetValueAsync(app.Services, "database-seed", seedState);
        }
    }
    catch (Exception ex) when (!builder.Configuration.GetValue("Database:FailStartupOnMigrationError", false))
    {
        app.Logger.LogWarning(
            ex,
            "Database migration and seed were skipped. Start PostgreSQL and restart the API to create or update the ERP schema from code.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        app.Logger.LogError(ex, "Database is unavailable while handling {Path}.", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Database unavailable",
            detail = "PostgreSQL is not reachable. Check the connection string in Erp.Api/.env and make sure the database service is running."
        });
    }
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<InternalErpService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "ERP API is running over HTTPS with HTTP/3 enabled. REST: /swagger. Internal gRPC: internalerp.InternalErp.");

app.Run();

static bool IsDatabaseUnavailable(Exception ex)
{
    return ex is NpgsqlException ||
           ex.InnerException is NpgsqlException ||
           ex.InnerException?.InnerException is NpgsqlException;
}

static string? NormalizeRedisConfiguration(string? configuration)
{
    if (string.IsNullOrWhiteSpace(configuration))
    {
        return null;
    }

    if (!Uri.TryCreate(configuration, UriKind.Absolute, out var uri) ||
        (uri.Scheme is not "redis" and not "rediss"))
    {
        return configuration;
    }

    var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    var options = new List<string> { host, "abortConnect=False" };
    if (uri.Scheme == "rediss")
    {
        options.Add("ssl=True");
    }

    if (!string.IsNullOrWhiteSpace(uri.UserInfo))
    {
        var parts = uri.UserInfo.Split(':', 2);
        var password = parts.Length == 2 ? parts[1] : parts[0];
        if (!string.IsNullOrWhiteSpace(password))
        {
            options.Add($"password={Uri.UnescapeDataString(password)}");
        }
    }

    return string.Join(',', options);
}

static int GetAvailablePort(int preferredPort)
{
    for (var port = preferredPort; port < preferredPort + 20; port++)
    {
        if (IsPortAvailable(port))
        {
            return port;
        }
    }

    return preferredPort;
}

static bool IsPortAvailable(int port)
{
    try
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        return true;
    }
    catch (System.Net.Sockets.SocketException)
    {
        return false;
    }
}

public partial class Program;
