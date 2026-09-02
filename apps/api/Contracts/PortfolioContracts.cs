using System.ComponentModel.DataAnnotations;
using PlanVest.Api.Models;

namespace PlanVest.Api.Contracts;

public sealed record CreateAccountRequest(
    [property: Required, StringLength(80, MinimumLength = 2)] string Name,
    [property: EnumDataType(typeof(AccountType))] AccountType AccountType);

public sealed record UpdateAccountRequest(
    [property: Required, StringLength(80, MinimumLength = 2)] string Name,
    [property: EnumDataType(typeof(AccountType))] AccountType AccountType);

public sealed record UpsertHoldingRequest(
    [property: Required, StringLength(20, MinimumLength = 1)] string Symbol,
    [property: Required, StringLength(120, MinimumLength = 2)] string AssetName,
    [property: EnumDataType(typeof(AssetClass))] AssetClass AssetClass,
    [property: Range(typeof(decimal), "0.000001", "1000000000")] decimal Quantity,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal AverageCost,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal CurrentPrice);

public sealed record CreateTransactionRequest(
    [property: EnumDataType(typeof(TransactionType))] TransactionType Type,
    Guid? HoldingId,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal Quantity,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal Price,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal Amount,
    DateOnly TransactionDate,
    [property: StringLength(240)] string? Note);

public sealed record HoldingResponse(
    Guid Id,
    Guid InvestmentAccountId,
    string Symbol,
    string AssetName,
    AssetClass AssetClass,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal MarketValue,
    DateTimeOffset UpdatedAt);

public sealed record TransactionResponse(
    Guid Id,
    Guid InvestmentAccountId,
    Guid? HoldingId,
    TransactionType Type,
    decimal Quantity,
    decimal Price,
    decimal Amount,
    DateOnly TransactionDate,
    string? Note);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    AccountType AccountType,
    string BaseCurrency,
    decimal MarketValue,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<HoldingResponse> Holdings,
    IReadOnlyCollection<TransactionResponse> Transactions);

public sealed record AllocationItemResponse(
    AssetClass AssetClass,
    decimal MarketValue,
    decimal Percentage);

public sealed record PortfolioSummaryResponse(
    decimal TotalMarketValue,
    int AccountCount,
    int HoldingCount,
    IReadOnlyCollection<AllocationItemResponse> Allocation);
