using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Infrastructure;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth").WithTags("Authentication");

        auth.MapPost("/register", Register)
            .AddEndpointFilter<RequestValidationFilter<RegisterRequest>>();
        auth.MapPost("/login", Login)
            .AddEndpointFilter<RequestValidationFilter<LoginRequest>>();
        auth.MapPost("/logout", Logout).RequireAuthorization();
        auth.MapGet("/me", Me).RequireAuthorization();
        auth.MapPost("/demo-session", DemoSession);
        return app;
    }

    private static async Task<IResult> Register(RegisterRequest request, AppDbContext db,
        IPasswordHasher<ApplicationUser> passwordHasher, TokenService tokens)
    {
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
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
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return AuthResult(user, tokens, StatusCodes.Status201Created);
    }

    private static async Task<IResult> Login(LoginRequest request, AppDbContext db,
        IPasswordHasher<ApplicationUser> passwordHasher, TokenService tokens)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(value => value.NormalizedEmail == normalizedEmail);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
            return Results.Problem(title: "Invalid credentials",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return AuthResult(user, tokens);
    }

    private static async Task<IResult> Logout(System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.SingleAsync(value => value.Id == userId);
        user.TokenVersion++;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> Me(System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(new UserResponse(user.Id, user.DisplayName, user.Email));
    }

    private static async Task<IResult> DemoSession(DemoSeeder seeder, TokenService tokens)
    {
        var user = await seeder.CreateWorkspaceAsync();
        return AuthResult(user, tokens, StatusCodes.Status201Created);
    }

    private static IResult AuthResult(ApplicationUser user, TokenService tokens,
        int statusCode = StatusCodes.Status200OK)
    {
        var issued = tokens.Create(user);
        var response = new AuthResponse(issued.Token, issued.ExpiresAt,
            new UserResponse(user.Id, user.DisplayName, user.Email));
        return statusCode == StatusCodes.Status201Created
            ? Results.Created("/api/auth/me", response)
            : Results.Ok(response);
    }
}
