using PlanVest.Api.Contracts;
using PlanVest.Api.Models;

namespace PlanVest.Api.Services;

public static class GoalPlanningService
{
    public static GoalResponse ToResponse(FinancialGoal goal, DateOnly? today = null)
    {
        var currentDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var months = Math.Max(0,
            (goal.TargetDate.Year - currentDate.Year) * 12 + goal.TargetDate.Month - currentDate.Month);
        var projected = PlanningCalculator.FutureValue(goal.CurrentAmount, goal.MonthlyContribution,
            goal.AssumedAnnualReturn, months);
        var required = months == 0
            ? decimal.Max(0m, goal.TargetAmount - goal.CurrentAmount)
            : PlanningCalculator.RequiredMonthlyContribution(goal.TargetAmount, goal.CurrentAmount,
                goal.AssumedAnnualReturn, months);
        var progress = goal.TargetAmount == 0
            ? 0
            : decimal.Min(100m, decimal.Round(goal.CurrentAmount / goal.TargetAmount * 100m, 2));

        return new GoalResponse(
            goal.Id,
            goal.Name,
            goal.GoalType,
            goal.TargetAmount,
            goal.CurrentAmount,
            goal.TargetDate,
            goal.MonthlyContribution,
            goal.AssumedAnnualReturn,
            goal.Status,
            progress,
            projected,
            required);
    }
}
