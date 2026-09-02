using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var databaseConnection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
if (!Path.IsPathRooted(databaseConnection.DataSource))
    databaseConnection.DataSource = Path.GetFullPath(databaseConnection.DataSource,
        builder.Environment.ContentRootPath);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(databaseConnection.ConnectionString));
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
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(builder.Configuration["WebOrigin"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

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
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (database.Database.IsRelational()) await database.Database.MigrateAsync();
    else await database.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
