using System.ComponentModel.DataAnnotations;
using PlanVest.Api.Models;

namespace PlanVest.Api.Contracts;

public sealed record RiskOptionResponse(string Id, string Label, int Score);
public sealed record RiskQuestionResponse(
    string Id,
    string Category,
    string Prompt,
    IReadOnlyCollection<RiskOptionResponse> Options);

public sealed record SubmitRiskAssessmentRequest(
    [property: Required] Dictionary<string, string> Answers);

public sealed record RiskAssessmentResponse(
    Guid Id,
    string ScoringVersion,
    int TotalScore,
    RiskProfile RiskProfile,
    IReadOnlyDictionary<string, int> CategorySubscores,
    string Rationale,
    DateTimeOffset CreatedAt,
    string Disclaimer);

public sealed record ModelAllocationResponse(
    RiskProfile RiskProfile,
    decimal Equity,
    decimal FixedIncome,
    decimal Cash);

public sealed record AllocationComparisonItemResponse(
    string AssetClass,
    decimal CurrentPercentage,
    decimal TargetPercentage,
    decimal DifferencePercentagePoints,
    decimal ApproximateDollarGap);

public sealed record AllocationComparisonResponse(
    ModelAllocationResponse Model,
    IReadOnlyCollection<AllocationComparisonItemResponse> Items,
    string Disclaimer);

public sealed record UpsertGoalRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: EnumDataType(typeof(GoalType))] GoalType GoalType,
    [property: Range(typeof(decimal), "1", "1000000000")] decimal TargetAmount,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal CurrentAmount,
    DateOnly TargetDate,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal MonthlyContribution,
    [property: Range(typeof(decimal), "0", "30")] decimal AssumedAnnualReturn,
    [property: EnumDataType(typeof(GoalStatus))] GoalStatus Status = GoalStatus.Active);

public sealed record GoalResponse(
    Guid Id,
    string Name,
    GoalType GoalType,
    decimal TargetAmount,
    decimal CurrentAmount,
    DateOnly TargetDate,
    decimal MonthlyContribution,
    decimal AssumedAnnualReturn,
    GoalStatus Status,
    decimal ProgressPercentage,
    decimal ProjectedValue,
    decimal RequiredMonthlyContribution);

public sealed record FutureValueRequest(
    [property: Range(typeof(decimal), "0", "1000000000")] decimal Principal,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal MonthlyContribution,
    [property: Range(typeof(decimal), "0", "30")] decimal AnnualRatePercent,
    [property: Range(1, 600)] int Months);

public sealed record RequiredContributionRequest(
    [property: Range(typeof(decimal), "1", "1000000000")] decimal Target,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal Principal,
    [property: Range(typeof(decimal), "0", "30")] decimal AnnualRatePercent,
    [property: Range(1, 600)] int Months);

public sealed record SimulationResponse(decimal Value, string Disclaimer);

public sealed record DashboardResponse(
    UserResponse User,
    PortfolioSummaryResponse Portfolio,
    IReadOnlyCollection<AccountResponse> Accounts,
    RiskAssessmentResponse? LatestRiskAssessment,
    AllocationComparisonResponse AllocationComparison,
    IReadOnlyCollection<GoalResponse> Goals);
