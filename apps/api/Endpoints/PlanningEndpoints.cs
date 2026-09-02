using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Infrastructure;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Endpoints;

public static class PlanningEndpoints
{
    public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization().WithTags("Planning");
        api.MapGet("/risk/questions", () => Results.Ok(RiskScoringService.Questions));
        api.MapPost("/risk/assessments", SubmitRiskAssessment)
            .AddEndpointFilter<RequestValidationFilter<SubmitRiskAssessmentRequest>>();
        api.MapGet("/risk/latest", GetLatestRiskAssessment);
        api.MapGet("/plan/allocation-comparison", GetAllocationComparison);
        api.MapGet("/goals", GetGoals);
        api.MapPost("/goals", CreateGoal)
            .AddEndpointFilter<RequestValidationFilter<UpsertGoalRequest>>();
        api.MapPut("/goals/{id:guid}", UpdateGoal)
            .AddEndpointFilter<RequestValidationFilter<UpsertGoalRequest>>();
        api.MapDelete("/goals/{id:guid}", DeleteGoal);
        api.MapPost("/simulations/future-value", FutureValue)
            .AddEndpointFilter<RequestValidationFilter<FutureValueRequest>>();
        api.MapPost("/simulations/required-contribution", RequiredContribution)
            .AddEndpointFilter<RequestValidationFilter<RequiredContributionRequest>>();
        return app;
    }

    private static async Task<IResult> SubmitRiskAssessment(SubmitRiskAssessmentRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        RiskScoreResult score;
        try
        {
            score = RiskScoringService.Calculate(request.Answers);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Answers)] = [exception.Message]
            });
        }

        var assessment = new RiskAssessment
        {
            UserId = principal.GetUserId(),
            ScoringVersion = RiskScoringService.Version,
            AnswersJson = JsonSerializer.Serialize(request.Answers),
            TotalScore = score.TotalScore,
            RiskProfile = score.Profile,
            Rationale = score.Rationale
        };
        db.RiskAssessments.Add(assessment);
        await db.SaveChangesAsync();
        return Results.Created("/api/risk/latest", ToRiskResponse(assessment));
    }

    private static async Task<IResult> GetLatestRiskAssessment(
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var assessments = await db.RiskAssessments.AsNoTracking()
            .Where(value => value.UserId == userId)
            .ToListAsync();
        var assessment = assessments.MaxBy(value => value.CreatedAt);
        return assessment is null ? Results.NoContent() : Results.Ok(ToRiskResponse(assessment));
    }

    private static async Task<IResult> GetAllocationComparison(
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db,
        PortfolioService portfolios)
    {
        var userId = principal.GetUserId();
        var assessments = await db.RiskAssessments.AsNoTracking()
            .Where(value => value.UserId == userId)
            .ToListAsync();
        var profile = assessments.MaxBy(value => value.CreatedAt)?.RiskProfile ?? RiskProfile.Balanced;
        var summary = await portfolios.GetSummaryAsync(userId);
        return Results.Ok(PortfolioService.CompareAllocation(summary, profile));
    }

    private static async Task<IResult> GetGoals(System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userId = principal.GetUserId();
        var goals = await db.FinancialGoals.AsNoTracking().Where(value => value.UserId == userId)
            .OrderBy(value => value.Status).ThenBy(value => value.TargetDate).ToListAsync();
        return Results.Ok(goals.Select(value => GoalPlanningService.ToResponse(value)).ToArray());
    }

    private static async Task<IResult> CreateGoal(UpsertGoalRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        if (request.TargetDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            return InvalidTargetDate();
        var goal = new FinancialGoal { UserId = principal.GetUserId(), Name = "" };
        Apply(goal, request);
        db.FinancialGoals.Add(goal);
        await db.SaveChangesAsync();
        return Results.Created($"/api/goals/{goal.Id}", GoalPlanningService.ToResponse(goal));
    }

    private static async Task<IResult> UpdateGoal(Guid id, UpsertGoalRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        if (request.TargetDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            return InvalidTargetDate();
        var userId = principal.GetUserId();
        var goal = await db.FinancialGoals
            .SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId);
        if (goal is null) return Results.NotFound();
        Apply(goal, request);
        await db.SaveChangesAsync();
        return Results.Ok(GoalPlanningService.ToResponse(goal));
    }

    private static async Task<IResult> DeleteGoal(Guid id,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var goal = await db.FinancialGoals
            .SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId);
        if (goal is null) return Results.NotFound();
        db.FinancialGoals.Remove(goal);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static IResult FutureValue(FutureValueRequest request) => Results.Ok(new SimulationResponse(
        PlanningCalculator.FutureValue(request.Principal, request.MonthlyContribution,
            request.AnnualRatePercent, request.Months),
        "Projection based on your assumptions; returns are not guaranteed."));

    private static IResult RequiredContribution(RequiredContributionRequest request) => Results.Ok(
        new SimulationResponse(PlanningCalculator.RequiredMonthlyContribution(request.Target,
                request.Principal, request.AnnualRatePercent, request.Months),
            "Estimate based on your assumptions; returns are not guaranteed."));

    internal static RiskAssessmentResponse ToRiskResponse(RiskAssessment assessment)
    {
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(assessment.AnswersJson) ?? [];
        var score = RiskScoringService.Calculate(answers);
        return new RiskAssessmentResponse(
            assessment.Id,
            assessment.ScoringVersion,
            assessment.TotalScore,
            assessment.RiskProfile,
            score.CategorySubscores,
            assessment.Rationale,
            assessment.CreatedAt,
            RiskScoringService.Disclaimer);
    }

    private static void Apply(FinancialGoal goal, UpsertGoalRequest request)
    {
        goal.Name = request.Name.Trim();
        goal.GoalType = request.GoalType;
        goal.TargetAmount = request.TargetAmount;
        goal.CurrentAmount = request.CurrentAmount;
        goal.TargetDate = request.TargetDate;
        goal.MonthlyContribution = request.MonthlyContribution;
        goal.AssumedAnnualReturn = request.AssumedAnnualReturn;
        goal.Status = request.Status;
        goal.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static IResult InvalidTargetDate() => Results.ValidationProblem(
        new Dictionary<string, string[]>
        {
            [nameof(UpsertGoalRequest.TargetDate)] = ["Target date must be in the future."]
        });
}
