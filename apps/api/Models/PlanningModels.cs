namespace PlanVest.Api.Models;

public enum RiskProfile
{
    Conservative,
    Balanced,
    Growth
}

public enum GoalType
{
    EmergencyFund,
    Home,
    Education,
    Retirement,
    MajorPurchase,
    Other
}

public enum GoalStatus
{
    Active,
    Archived
}

public sealed class RiskAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ScoringVersion { get; set; } = "1.0";
    public required string AnswersJson { get; set; }
    public int TotalScore { get; set; }
    public RiskProfile RiskProfile { get; set; }
    public required string Rationale { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public GoalType GoalType { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly TargetDate { get; set; }
    public decimal MonthlyContribution { get; set; }
    public decimal AssumedAnnualReturn { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
