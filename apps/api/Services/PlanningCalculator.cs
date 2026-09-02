namespace PlanVest.Api.Services;

public static class PlanningCalculator
{
    public static decimal FutureValue(decimal principal, decimal monthlyContribution,
        decimal annualRatePercent, int months)
    {
        if (principal < 0 || monthlyContribution < 0) throw new ArgumentOutOfRangeException();
        if (annualRatePercent < 0 || months < 0) throw new ArgumentOutOfRangeException();
        if (months == 0) return decimal.Round(principal, 2);

        var monthlyRate = annualRatePercent / 100m / 12m;
        if (monthlyRate == 0)
            return decimal.Round(principal + monthlyContribution * months, 2);

        var growthFactor = Pow(1m + monthlyRate, months);
        var result = principal * growthFactor
            + monthlyContribution * ((growthFactor - 1m) / monthlyRate);
        return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal RequiredMonthlyContribution(decimal target, decimal principal,
        decimal annualRatePercent, int months)
    {
        if (target <= 0 || principal < 0 || annualRatePercent < 0 || months <= 0)
            throw new ArgumentOutOfRangeException();

        var monthlyRate = annualRatePercent / 100m / 12m;
        if (monthlyRate == 0)
            return decimal.Max(0m, decimal.Round((target - principal) / months, 2));

        var growthFactor = Pow(1m + monthlyRate, months);
        var remaining = target - principal * growthFactor;
        if (remaining <= 0) return 0m;
        return decimal.Round(remaining * monthlyRate / (growthFactor - 1m), 2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++) result *= value;
        return result;
    }
}
