using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlanVest.Api.Data;
using PlanVest.Api.Endpoints;
using PlanVest.Api.Infrastructure;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var platformPort = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(platformPort))
{
    if (!int.TryParse(platformPort, out var port) || port is < 1 or > 65535)
        throw new InvalidOperationException("PORT must be a valid TCP port.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(databaseConnectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

var useSqlite = databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
if (useSqlite)
{
    var sqliteConnection = new SqliteConnectionStringBuilder(databaseConnectionString);
    if (!Path.IsPathRooted(sqliteConnection.DataSource))
        sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource,
            builder.Environment.ContentRootPath);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConnection.ConnectionString));
}
else if (databaseProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(databaseConnectionString));
}
else
{
    throw new InvalidOperationException(
        $"Unsupported Database:Provider '{databaseProvider}'. Use Sqlite or PostgreSql.");
}
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var webOrigin = builder.Configuration["WebOrigin"]?.Trim();
if (string.IsNullOrWhiteSpace(webOrigin))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("WebOrigin must be explicitly configured outside Development and Testing.");
    webOrigin = "http://localhost:3000";
}
if (!Uri.TryCreate(webOrigin, UriKind.Absolute, out var parsedWebOrigin)
    || parsedWebOrigin.Scheme is not ("http" or "https")
    || parsedWebOrigin.AbsolutePath != "/"
    || !string.IsNullOrEmpty(parsedWebOrigin.Query)
    || !string.IsNullOrEmpty(parsedWebOrigin.Fragment))
    throw new InvalidOperationException("WebOrigin must be one absolute HTTP(S) origin without a path, query, or fragment.");
webOrigin = webOrigin.TrimEnd('/');
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(webOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var isRailway = builder.Configuration["Hosting:Provider"]
    ?.Equals("Railway", StringComparison.OrdinalIgnoreCase) == true;
if (isRailway)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardedForHeaderName = "X-Real-IP";
        options.ForwardLimit = 1;
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            IPAddress.Parse("100.0.0.0"), 8));
    });
}

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (!builder.Environment.IsDevelopment()
    && jwtKey.StartsWith("development-only-", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException(
        "The development JWT signing key cannot be used outside Development.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var version = context.Principal?.FindFirst("token_version")?.Value;
                if (!Guid.TryParse(subject, out var userId) || !int.TryParse(version, out var tokenVersion))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var currentVersion = await db.Users.AsNoTracking()
                    .Where(value => value.Id == userId)
                    .Select(value => (int?)value.TokenVersion)
                    .SingleOrDefaultAsync();
                if (currentVersion is null || currentVersion.Value != tokenVersion)
                    context.Fail("The session is no longer active.");
            }
        };
    });
builder.Services.AddAuthorization();
var authPermitLimit = Math.Max(1,
    builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (isRailway) app.UseForwardedHeaders();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPortfolioEndpoints();
app.MapPlanningEndpoints();
app.MapDashboardEndpoints();
app.MapGet("/api/health", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" })
        : Results.Problem(title: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable));

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useSqlite) await database.Database.EnsureCreatedAsync();
    else await database.Database.MigrateAsync();
}

app.Run();

public partial class Program;
