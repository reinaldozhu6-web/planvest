using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanVest.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609010002_MvpDomain")]
public partial class MvpDomain : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsDemo", table: "Users", type: "INTEGER",
            nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>(name: "TokenVersion", table: "Users", type: "INTEGER",
            nullable: false, defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "FinancialGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                GoalType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TargetDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                MonthlyContribution = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                AssumedAnnualReturn = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialGoals", value => value.Id);
                table.ForeignKey("FK_FinancialGoals_Users_UserId", value => value.UserId,
                    "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InvestmentAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                AccountType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvestmentAccounts", value => value.Id);
                table.ForeignKey("FK_InvestmentAccounts_Users_UserId", value => value.UserId,
                    "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RiskAssessments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                ScoringVersion = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                AnswersJson = table.Column<string>(type: "TEXT", nullable: false),
                TotalScore = table.Column<int>(type: "INTEGER", nullable: false),
                RiskProfile = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                Rationale = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RiskAssessments", value => value.Id);
                table.ForeignKey("FK_RiskAssessments_Users_UserId", value => value.UserId,
                    "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Holdings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                InvestmentAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AssetName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                AssetClass = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                AverageCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrentPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Holdings", value => value.Id);
                table.ForeignKey("FK_Holdings_InvestmentAccounts_InvestmentAccountId",
                    value => value.InvestmentAccountId, "InvestmentAccounts", "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                InvestmentAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                HoldingId = table.Column<Guid>(type: "TEXT", nullable: true),
                Type = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", value => value.Id);
                table.ForeignKey("FK_Transactions_Holdings_HoldingId", value => value.HoldingId,
                    "Holdings", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Transactions_InvestmentAccounts_InvestmentAccountId",
                    value => value.InvestmentAccountId, "InvestmentAccounts", "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_FinancialGoals_UserId", "FinancialGoals", "UserId");
        migrationBuilder.CreateIndex("IX_Holdings_InvestmentAccountId", "Holdings", "InvestmentAccountId");
        migrationBuilder.CreateIndex("IX_InvestmentAccounts_UserId", "InvestmentAccounts", "UserId");
        migrationBuilder.CreateIndex("IX_RiskAssessments_UserId_CreatedAt", "RiskAssessments",
            new[] { "UserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Transactions_HoldingId", "Transactions", "HoldingId");
        migrationBuilder.CreateIndex("IX_Transactions_InvestmentAccountId", "Transactions", "InvestmentAccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("FinancialGoals");
        migrationBuilder.DropTable("RiskAssessments");
        migrationBuilder.DropTable("Transactions");
        migrationBuilder.DropTable("Holdings");
        migrationBuilder.DropTable("InvestmentAccounts");
        migrationBuilder.DropColumn("IsDemo", "Users");
        migrationBuilder.DropColumn("TokenVersion", "Users");
    }
}
