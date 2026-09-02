using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Infrastructure;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", GetDashboard)
            .RequireAuthorization().WithTags("Dashboard");
        return app;
    }

    private static async Task<IResult> GetDashboard(
        System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db,
        PortfolioService portfolios)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId);
        if (user is null) return Results.Unauthorized();

        var accounts = await portfolios.GetAccountsAsync(userId);
        var summary = await portfolios.GetSummaryAsync(userId);
        var assessments = await db.RiskAssessments.AsNoTracking()
            .Where(value => value.UserId == userId)
            .ToListAsync();
        var assessment = assessments.MaxBy(value => value.CreatedAt);
        var profile = assessment?.RiskProfile ?? RiskProfile.Balanced;
        var goals = await db.FinancialGoals.AsNoTracking()
            .Where(value => value.UserId == userId)
            .OrderBy(value => value.Status).ThenBy(value => value.TargetDate)
            .ToListAsync();

        return Results.Ok(new DashboardResponse(
            new UserResponse(user.Id, user.DisplayName, user.Email),
            summary,
            accounts,
            assessment is null ? null : PlanningEndpoints.ToRiskResponse(assessment),
            PortfolioService.CompareAllocation(summary, profile),
            goals.Select(value => GoalPlanningService.ToResponse(value)).ToArray()));
    }
}
