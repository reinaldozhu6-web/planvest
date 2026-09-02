using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Tests;

public sealed class GoalPlanningServiceTests
{
    [Fact]
    public void ToResponse_CapsVisualProgressAtOneHundredPercent()
    {
        var goal = new FinancialGoal
        {
            UserId = Guid.NewGuid(),
            Name = "Synthetic goal",
            GoalType = GoalType.Other,
            TargetAmount = 1_000m,
            CurrentAmount = 1_250m,
            TargetDate = new DateOnly(2030, 1, 1),
            MonthlyContribution = 0m,
            AssumedAnnualReturn = 0m
        };

        var result = GoalPlanningService.ToResponse(goal, new DateOnly(2029, 1, 1));

        Assert.Equal(100m, result.ProgressPercentage);
        Assert.Equal(1_250m, result.ProjectedValue);
        Assert.Equal(0m, result.RequiredMonthlyContribution);
    }
}
