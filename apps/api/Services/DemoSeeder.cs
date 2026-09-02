using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using PlanVest.Api.Data;
using PlanVest.Api.Models;

namespace PlanVest.Api.Services;

public sealed class DemoSeeder(AppDbContext db, IPasswordHasher<ApplicationUser> passwordHasher)
{
    public async Task<ApplicationUser> CreateWorkspaceAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var user = new ApplicationUser
        {
            DisplayName = "Alex Chen",
            Email = $"demo-{suffix}@planvest.local",
            NormalizedEmail = $"DEMO-{suffix.ToUpperInvariant()}@PLANVEST.LOCAL",
            PasswordHash = string.Empty,
            IsDemo = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        var tfsa = new InvestmentAccount
        {
            UserId = user.Id,
            Name = "Long-term TFSA",
            AccountType = AccountType.Tfsa,
            Holdings =
            [
                Holding("XEQT", "iShares Core Equity ETF", AssetClass.InternationalEquity, 520m, 54.64m, 55.10m),
                Holding("VFV", "Vanguard S&P 500 ETF", AssetClass.UsEquity, 120m, 149.50m, 158.20m)
            ]
        };
        var rrsp = new InvestmentAccount
        {
            UserId = user.Id,
            Name = "Retirement RRSP",
            AccountType = AccountType.Rrsp,
            Holdings =
            [
                Holding("XBB", "iShares Canadian Bond ETF", AssetClass.FixedIncome, 410m, 30.20m, 30.82m),
                Holding("XIC", "iShares Core S&P/TSX ETF", AssetClass.CanadianEquity, 260m, 40.10m, 41.59m),
                Holding("CASH", "High-interest savings", AssetClass.Cash, 1m, 3980m, 3980m)
            ]
        };

        var answers = new Dictionary<string, string>
        {
            ["timeHorizon"] = "overSeven",
            ["incomeStability"] = "mostlyStable",
            ["emergencyFund"] = "overThree",
            ["knowledge"] = "some",
            ["lossReaction"] = "hold",
            ["liquidity"] = "low",
            ["objective"] = "growth"
        };
        var risk = RiskScoringService.Calculate(answers);
        var assessment = new RiskAssessment
        {
            UserId = user.Id,
            AnswersJson = JsonSerializer.Serialize(answers),
            TotalScore = risk.TotalScore,
            RiskProfile = risk.Profile,
            Rationale = risk.Rationale
        };
        var goal = new FinancialGoal
        {
            UserId = user.Id,
            Name = "Home deposit",
            GoalType = GoalType.Home,
            TargetAmount = 50_000m,
            CurrentAmount = 32_100m,
            TargetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            MonthlyContribution = 650m,
            AssumedAnnualReturn = 4m
        };

        db.AddRange(user, tfsa, rrsp, assessment, goal);
        await db.SaveChangesAsync();
        return user;
    }

    private static Holding Holding(string symbol, string name, AssetClass assetClass,
        decimal quantity, decimal averageCost, decimal currentPrice) => new()
        {
            Symbol = symbol,
            AssetName = name,
            AssetClass = assetClass,
            Quantity = quantity,
            AverageCost = averageCost,
            CurrentPrice = currentPrice
        };
}
