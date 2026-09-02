using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Models;

namespace PlanVest.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<InvestmentAccount> InvestmentAccounts => Set<InvestmentAccount>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<PortfolioTransaction> Transactions => Set<PortfolioTransaction>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApplicationUser>();
        user.HasKey(value => value.Id);
        user.HasIndex(value => value.NormalizedEmail).IsUnique();
        user.Property(value => value.DisplayName).HasMaxLength(80);
        user.Property(value => value.Email).HasMaxLength(254);
        user.Property(value => value.NormalizedEmail).HasMaxLength(254);
        user.Property(value => value.PasswordHash).HasMaxLength(1000);

        var account = modelBuilder.Entity<InvestmentAccount>();
        account.HasKey(value => value.Id);
        account.HasIndex(value => value.UserId);
        account.Property(value => value.Name).HasMaxLength(80);
        account.Property(value => value.AccountType).HasConversion<string>().HasMaxLength(32);
        account.Property(value => value.BaseCurrency).HasMaxLength(3);
        account.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var holding = modelBuilder.Entity<Holding>();
        holding.HasKey(value => value.Id);
        holding.HasIndex(value => value.InvestmentAccountId);
        holding.Property(value => value.Symbol).HasMaxLength(20);
        holding.Property(value => value.AssetName).HasMaxLength(120);
        holding.Property(value => value.AssetClass).HasConversion<string>().HasMaxLength(32);
        holding.Property(value => value.Quantity).HasPrecision(18, 6);
        holding.Property(value => value.AverageCost).HasPrecision(18, 2);
        holding.Property(value => value.CurrentPrice).HasPrecision(18, 2);
        holding.HasOne(value => value.InvestmentAccount).WithMany(value => value.Holdings)
            .HasForeignKey(value => value.InvestmentAccountId).OnDelete(DeleteBehavior.Cascade);

        var transaction = modelBuilder.Entity<PortfolioTransaction>();
        transaction.HasKey(value => value.Id);
        transaction.HasIndex(value => value.InvestmentAccountId);
        transaction.Property(value => value.Type).HasConversion<string>().HasMaxLength(24);
        transaction.Property(value => value.Quantity).HasPrecision(18, 6);
        transaction.Property(value => value.Price).HasPrecision(18, 2);
        transaction.Property(value => value.Amount).HasPrecision(18, 2);
        transaction.Property(value => value.Note).HasMaxLength(240);
        transaction.HasOne(value => value.InvestmentAccount).WithMany(value => value.Transactions)
            .HasForeignKey(value => value.InvestmentAccountId).OnDelete(DeleteBehavior.Cascade);
        transaction.HasOne(value => value.Holding).WithMany().HasForeignKey(value => value.HoldingId)
            .OnDelete(DeleteBehavior.SetNull);

        var assessment = modelBuilder.Entity<RiskAssessment>();
        assessment.HasKey(value => value.Id);
        assessment.HasIndex(value => new { value.UserId, value.CreatedAt });
        assessment.Property(value => value.ScoringVersion).HasMaxLength(16);
        assessment.Property(value => value.RiskProfile).HasConversion<string>().HasMaxLength(24);
        assessment.Property(value => value.Rationale).HasMaxLength(1000);
        assessment.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var goal = modelBuilder.Entity<FinancialGoal>();
        goal.HasKey(value => value.Id);
        goal.HasIndex(value => value.UserId);
        goal.Property(value => value.Name).HasMaxLength(100);
        goal.Property(value => value.GoalType).HasConversion<string>().HasMaxLength(32);
        goal.Property(value => value.Status).HasConversion<string>().HasMaxLength(24);
        goal.Property(value => value.TargetAmount).HasPrecision(18, 2);
        goal.Property(value => value.CurrentAmount).HasPrecision(18, 2);
        goal.Property(value => value.MonthlyContribution).HasPrecision(18, 2);
        goal.Property(value => value.AssumedAnnualReturn).HasPrecision(5, 2);
        goal.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
