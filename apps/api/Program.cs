using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(builder.Configuration["WebOrigin"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("auth", limiter =>
{
    limiter.PermitLimit = 10;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");

auth.MapPost("/register", async (RegisterRequest request, AppDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher, TokenService tokens) =>
{
    var normalizedEmail = request.Email.Trim().ToUpperInvariant();
    if (await db.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail))
        return Results.Conflict(new ProblemDetails
        {
            Title = "Email already registered",
            Detail = "Use a different email address or sign in.",
            Status = StatusCodes.Status409Conflict
        });

    var user = new ApplicationUser
    {
        DisplayName = request.DisplayName.Trim(),
        Email = request.Email.Trim(),
        NormalizedEmail = normalizedEmail,
        PasswordHash = string.Empty
    };
    user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var issued = tokens.Create(user);
    return Results.Created("/api/auth/me", new AuthResponse(
        issued.Token, issued.ExpiresAt, new UserResponse(user.Id, user.DisplayName, user.Email)));
});

auth.MapPost("/login", async (LoginRequest request, AppDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher, TokenService tokens) =>
{
    var normalizedEmail = request.Email.Trim().ToUpperInvariant();
    var user = await db.Users.SingleOrDefaultAsync(value => value.NormalizedEmail == normalizedEmail);
    if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
        == PasswordVerificationResult.Failed)
        return Results.Problem(title: "Invalid credentials", statusCode: StatusCodes.Status401Unauthorized);

    user.LastLoginAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    var issued = tokens.Create(user);
    return Results.Ok(new AuthResponse(
        issued.Token, issued.ExpiresAt, new UserResponse(user.Id, user.DisplayName, user.Email)));
});

auth.MapPost("/logout", () => Results.NoContent()).RequireAuthorization();

auth.MapGet("/me", async (System.Security.Claims.ClaimsPrincipal principal, AppDbContext db) =>
{
    var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(subject, out var userId)) return Results.Unauthorized();
    var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new UserResponse(user.Id, user.DisplayName, user.Email));
}).RequireAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
