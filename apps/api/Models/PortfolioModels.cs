namespace PlanVest.Api.Models;

public enum AccountType
{
    Tfsa,
    Rrsp,
    Fhsa,
    NonRegistered,
    Cash
}

public enum AssetClass
{
    CanadianEquity,
    UsEquity,
    InternationalEquity,
    FixedIncome,
    Cash,
    Other
}

public enum TransactionType
{
    Buy,
    Sell,
    Deposit,
    Withdrawal
}

public sealed class InvestmentAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public AccountType AccountType { get; set; }
    public string BaseCurrency { get; set; } = "CAD";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Holding> Holdings { get; set; } = [];
    public List<PortfolioTransaction> Transactions { get; set; } = [];
}

public sealed class Holding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvestmentAccountId { get; set; }
    public required string Symbol { get; set; }
    public required string AssetName { get; set; }
    public AssetClass AssetClass { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InvestmentAccount InvestmentAccount { get; set; } = null!;
}

public sealed class PortfolioTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvestmentAccountId { get; set; }
    public Guid? HoldingId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InvestmentAccount InvestmentAccount { get; set; } = null!;
    public Holding? Holding { get; set; }
}
