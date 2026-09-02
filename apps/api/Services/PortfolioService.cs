using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Models;

namespace PlanVest.Api.Services;

public sealed class PortfolioService(AppDbContext db)
{
    public async Task<IReadOnlyCollection<AccountResponse>> GetAccountsAsync(Guid userId)
    {
        var accounts = await db.InvestmentAccounts.AsNoTracking()
            .Where(value => value.UserId == userId)
            .Include(value => value.Holdings)
            .Include(value => value.Transactions)
            .ToListAsync();

        return accounts.OrderBy(value => value.CreatedAt)
            .Select(ResponseMapper.Account).ToArray();
    }

    public async Task<PortfolioSummaryResponse> GetSummaryAsync(Guid userId)
    {
        var accountCount = await db.InvestmentAccounts.AsNoTracking()
            .CountAsync(value => value.UserId == userId);
        var holdings = await db.Holdings.AsNoTracking()
            .Where(value => value.InvestmentAccount.UserId == userId)
            .ToListAsync();

        return CalculateSummary(holdings, accountCount);
    }

    public static PortfolioSummaryResponse CalculateSummary(IEnumerable<Holding> source,
        int? accountCount = null)
    {
        var holdings = source.ToArray();
        var total = holdings.Sum(value => value.Quantity * value.CurrentPrice);
        var allocation = holdings
            .GroupBy(value => value.AssetClass)
            .Select(group => new AllocationItemResponse(
                group.Key,
                decimal.Round(group.Sum(value => value.Quantity * value.CurrentPrice), 2,
                    MidpointRounding.AwayFromZero),
                total == 0 ? 0 : decimal.Round(
                    group.Sum(value => value.Quantity * value.CurrentPrice) / total * 100m, 2,
                    MidpointRounding.AwayFromZero)))
            .OrderByDescending(value => value.MarketValue)
            .ToArray();

        return new PortfolioSummaryResponse(
            decimal.Round(total, 2, MidpointRounding.AwayFromZero),
            accountCount ?? holdings.Select(value => value.InvestmentAccountId).Distinct().Count(),
            holdings.Length,
            allocation);
    }

    public static AllocationComparisonResponse CompareAllocation(
        PortfolioSummaryResponse summary,
        RiskProfile profile)
    {
        var model = RiskScoringService.GetModelAllocation(profile);
        var equity = summary.Allocation
            .Where(value => value.AssetClass is AssetClass.CanadianEquity
                or AssetClass.UsEquity or AssetClass.InternationalEquity)
            .Sum(value => value.Percentage);
        var fixedIncome = summary.Allocation
            .Where(value => value.AssetClass == AssetClass.FixedIncome)
            .Sum(value => value.Percentage);
        var cash = summary.Allocation
            .Where(value => value.AssetClass is AssetClass.Cash or AssetClass.Other)
            .Sum(value => value.Percentage);

        var current = new Dictionary<string, decimal>
        {
            ["Equity"] = equity,
            ["Fixed income"] = fixedIncome,
            ["Cash and other"] = cash
        };
        var targets = new Dictionary<string, decimal>
        {
            ["Equity"] = model.Equity,
            ["Fixed income"] = model.FixedIncome,
            ["Cash and other"] = model.Cash
        };

        var items = targets.Select(target =>
        {
            var difference = decimal.Round(target.Value - current[target.Key], 2,
                MidpointRounding.AwayFromZero);
            return new AllocationComparisonItemResponse(
                target.Key,
                decimal.Round(current[target.Key], 2, MidpointRounding.AwayFromZero),
                target.Value,
                difference,
                decimal.Round(summary.TotalMarketValue * difference / 100m, 2,
                    MidpointRounding.AwayFromZero));
        }).ToArray();

        return new AllocationComparisonResponse(model, items,
            "Educational model allocation only. This is not investment advice.");
    }
}

public static class ResponseMapper
{
    public static HoldingResponse Holding(Holding value) => new(
        value.Id,
        value.InvestmentAccountId,
        value.Symbol,
        value.AssetName,
        value.AssetClass,
        value.Quantity,
        value.AverageCost,
        value.CurrentPrice,
        decimal.Round(value.Quantity * value.CurrentPrice, 2, MidpointRounding.AwayFromZero),
        value.UpdatedAt);

    public static TransactionResponse Transaction(PortfolioTransaction value) => new(
        value.Id,
        value.InvestmentAccountId,
        value.HoldingId,
        value.Type,
        value.Quantity,
        value.Price,
        value.Amount,
        value.TransactionDate,
        value.Note);

    public static AccountResponse Account(InvestmentAccount value) => new(
        value.Id,
        value.Name,
        value.AccountType,
        value.BaseCurrency,
        decimal.Round(value.Holdings.Sum(holding => holding.Quantity * holding.CurrentPrice), 2,
            MidpointRounding.AwayFromZero),
        value.CreatedAt,
        value.Holdings.OrderByDescending(holding => holding.Quantity * holding.CurrentPrice)
            .Select(Holding).ToArray(),
        value.Transactions.OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Select(Transaction).ToArray());
}
