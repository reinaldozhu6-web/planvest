using PlanVest.Api.Services;

namespace PlanVest.Api.Tests;

public sealed class PlanningCalculatorTests
{
    [Fact]
    public void FutureValue_WithZeroReturn_AddsContributionsWithoutDividingByZero()
    {
        var result = PlanningCalculator.FutureValue(10_000m, 500m, 0m, 12);
        Assert.Equal(16_000m, result);
    }

    [Fact]
    public void FutureValue_WithGrowth_UsesMonthlyCompounding()
    {
        var result = PlanningCalculator.FutureValue(10_000m, 500m, 6m, 12);
        Assert.Equal(16_784.56m, result);
    }

    [Fact]
    public void RequiredContribution_WhenPrincipalAlreadyReachesTarget_ReturnsZero()
    {
        var result = PlanningCalculator.RequiredMonthlyContribution(10_000m, 10_000m, 6m, 12);
        Assert.Equal(0m, result);
    }
}
