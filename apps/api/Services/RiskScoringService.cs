using PlanVest.Api.Contracts;
using PlanVest.Api.Models;

namespace PlanVest.Api.Services;

public sealed record RiskScoreResult(
    int TotalScore,
    RiskProfile Profile,
    IReadOnlyDictionary<string, int> CategorySubscores,
    string Rationale);

public static class RiskScoringService
{
    public const string Version = "1.0";
    public const string Disclaimer =
        "Educational risk model only. It is not a suitability determination or investment advice.";

    public static readonly IReadOnlyCollection<RiskQuestionResponse> Questions =
    [
        Question("timeHorizon", "Time horizon", "When do you expect to need most of this money?",
            ("under3", "Within 3 years", 0), ("threeToSeven", "In 3–7 years", 8),
            ("overSeven", "More than 7 years", 15)),
        Question("incomeStability", "Income stability", "How predictable is your household income?",
            ("variable", "Variable or uncertain", 0), ("mostlyStable", "Mostly stable", 6),
            ("veryStable", "Very stable", 10)),
        Question("emergencyFund", "Emergency readiness", "How many months of essential expenses are set aside?",
            ("underOne", "Less than 1 month", 0), ("oneToThree", "1–3 months", 6),
            ("overThree", "More than 3 months", 10)),
        Question("knowledge", "Investment knowledge", "How familiar are you with market volatility?",
            ("new", "New to investing", 0), ("some", "Some practical experience", 5),
            ("experienced", "Experienced through market cycles", 10)),
        Question("lossReaction", "Loss tolerance", "If your portfolio fell 20%, what would you most likely do?",
            ("sell", "Sell to prevent further losses", 0), ("hold", "Hold the current plan", 12),
            ("add", "Add while prices are lower", 20)),
        Question("liquidity", "Liquidity need", "How much of this portfolio may be needed unexpectedly?",
            ("high", "A large portion", 0), ("medium", "A moderate portion", 8),
            ("low", "Very little", 15)),
        Question("objective", "Primary objective", "What is the main purpose of this portfolio?",
            ("preserve", "Preserve capital", 0), ("balanced", "Balance growth and stability", 10),
            ("growth", "Long-term growth", 20))
    ];

    public static RiskScoreResult Calculate(IReadOnlyDictionary<string, string> answers)
    {
        if (answers.Count != Questions.Count)
            throw new ArgumentException("Answer every risk question before submitting.", nameof(answers));

        var scores = new Dictionary<string, int>();
        var selectedLabels = new Dictionary<string, string>();
        foreach (var question in Questions)
        {
            if (!answers.TryGetValue(question.Id, out var optionId))
                throw new ArgumentException($"Missing answer for {question.Id}.", nameof(answers));
            var option = question.Options.SingleOrDefault(value => value.Id == optionId)
                ?? throw new ArgumentException($"Invalid answer for {question.Id}.", nameof(answers));
            scores[question.Category] = option.Score;
            selectedLabels[question.Category] = option.Label;
        }

        var total = scores.Values.Sum();
        var profile = total switch
        {
            <= 35 => RiskProfile.Conservative,
            <= 70 => RiskProfile.Balanced,
            _ => RiskProfile.Growth
        };
        var rationale = $"A {selectedLabels["Time horizon"].ToLowerInvariant()} time horizon and " +
            $"a likely response to losses of “{selectedLabels["Loss tolerance"]}” are the largest " +
            $"drivers of this {profile.ToString().ToLowerInvariant()} result.";

        return new RiskScoreResult(total, profile, scores, rationale);
    }

    public static ModelAllocationResponse GetModelAllocation(RiskProfile profile) => profile switch
    {
        RiskProfile.Conservative => new(profile, 35m, 55m, 10m),
        RiskProfile.Balanced => new(profile, 65m, 30m, 5m),
        RiskProfile.Growth => new(profile, 85m, 10m, 5m),
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    private static RiskQuestionResponse Question(string id, string category, string prompt,
        params (string Id, string Label, int Score)[] options) =>
        new(id, category, prompt,
            options.Select(value => new RiskOptionResponse(value.Id, value.Label, value.Score)).ToArray());
}
