using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Tests;

public sealed class PortfolioServiceTests
{
    [Fact]
    public void CalculateSummary_UsesDecimalMarketValuesAndPercentages()
    {
        var accountId = Guid.NewGuid();
        var holdings = new[]
        {
            Holding(accountId, AssetClass.CanadianEquity, 3.25m, 100.10m),
            Holding(accountId, AssetClass.FixedIncome, 7m, 25.50m)
        };

        var result = PortfolioService.CalculateSummary(holdings, accountCount: 1);

        Assert.Equal(503.83m, result.TotalMarketValue);
        Assert.Equal(64.57m, result.Allocation.Single(value =>
            value.AssetClass == AssetClass.CanadianEquity).Percentage);
        Assert.Equal(35.43m, result.Allocation.Single(value =>
            value.AssetClass == AssetClass.FixedIncome).Percentage);
        Assert.Equal(100m, result.Allocation.Sum(value => value.Percentage));
    }

    [Fact]
    public void CompareAllocation_ReturnsCurrentTargetAndDollarGap()
    {
        var summary = PortfolioService.CalculateSummary(
            [Holding(Guid.NewGuid(), AssetClass.UsEquity, 100m, 100m)], 1);

        var result = PortfolioService.CompareAllocation(summary, RiskProfile.Balanced);

        var equity = result.Items.Single(value => value.AssetClass == "Equity");
        Assert.Equal(100m, equity.CurrentPercentage);
        Assert.Equal(65m, equity.TargetPercentage);
        Assert.Equal(-3_500m, equity.ApproximateDollarGap);
    }

    private static Holding Holding(Guid accountId, AssetClass assetClass,
        decimal quantity, decimal price) => new()
    {
        InvestmentAccountId = accountId,
        Symbol = "TEST",
        AssetName = "Synthetic holding",
        AssetClass = assetClass,
        Quantity = quantity,
        CurrentPrice = price
    };
}
