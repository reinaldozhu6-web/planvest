using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Tests;

public sealed class RiskScoringServiceTests
{
    [Theory]
    [InlineData(35, RiskProfile.Conservative)]
    [InlineData(36, RiskProfile.Balanced)]
    [InlineData(70, RiskProfile.Balanced)]
    [InlineData(71, RiskProfile.Growth)]
    public void Calculate_UsesApprovedBoundaryThresholds(int expectedScore, RiskProfile expectedProfile)
    {
        var answers = AnswersForScore(expectedScore);

        var result = RiskScoringService.Calculate(answers);

        Assert.Equal(expectedScore, result.TotalScore);
        Assert.Equal(expectedProfile, result.Profile);
        Assert.Contains(expectedProfile.ToString(), result.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_WhenQuestionIsMissing_RejectsPartialAssessment()
    {
        var answers = AnswersForScore(35);
        answers.Remove("timeHorizon");

        Assert.Throws<ArgumentException>(() => RiskScoringService.Calculate(answers));
    }

    private static Dictionary<string, string> AnswersForScore(int score) => score switch
    {
        35 => Answers("overSeven", "variable", "overThree", "experienced", "sell", "high", "preserve"),
        36 => Answers("overSeven", "mostlyStable", "overThree", "some", "sell", "high", "preserve"),
        70 => Answers("overSeven", "veryStable", "overThree", "some", "add", "high", "balanced"),
        71 => Answers("overSeven", "mostlyStable", "overThree", "some", "add", "low", "preserve"),
        _ => throw new ArgumentOutOfRangeException(nameof(score))
    };

    private static Dictionary<string, string> Answers(string horizon, string income,
        string emergency, string knowledge, string loss, string liquidity, string objective) => new()
    {
        ["timeHorizon"] = horizon,
        ["incomeStability"] = income,
        ["emergencyFund"] = emergency,
        ["knowledge"] = knowledge,
        ["lossReaction"] = loss,
        ["liquidity"] = liquidity,
        ["objective"] = objective
    };
}
