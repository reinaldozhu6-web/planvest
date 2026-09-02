using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Contracts;
using PlanVest.Api.Data;
using PlanVest.Api.Infrastructure;
using PlanVest.Api.Models;
using PlanVest.Api.Services;

namespace PlanVest.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization().WithTags("Portfolio");
        api.MapGet("/accounts", GetAccounts);
        api.MapGet("/accounts/{id:guid}", GetAccount);
        api.MapPost("/accounts", CreateAccount)
            .AddEndpointFilter<RequestValidationFilter<CreateAccountRequest>>();
        api.MapPut("/accounts/{id:guid}", UpdateAccount)
            .AddEndpointFilter<RequestValidationFilter<UpdateAccountRequest>>();
        api.MapDelete("/accounts/{id:guid}", DeleteAccount);
        api.MapPost("/accounts/{accountId:guid}/holdings", CreateHolding)
            .AddEndpointFilter<RequestValidationFilter<UpsertHoldingRequest>>();
        api.MapPut("/holdings/{id:guid}", UpdateHolding)
            .AddEndpointFilter<RequestValidationFilter<UpsertHoldingRequest>>();
        api.MapDelete("/holdings/{id:guid}", DeleteHolding);
        api.MapPost("/accounts/{accountId:guid}/transactions", CreateTransaction)
            .AddEndpointFilter<RequestValidationFilter<CreateTransactionRequest>>();
        api.MapGet("/portfolio/summary", GetSummary);
        api.MapGet("/portfolio/allocation", GetAllocation);
        return app;
    }

    private static async Task<IResult> GetAccounts(System.Security.Claims.ClaimsPrincipal principal,
        PortfolioService portfolios) => Results.Ok(await portfolios.GetAccountsAsync(principal.GetUserId()));

    private static async Task<IResult> GetAccount(Guid id,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var account = await db.InvestmentAccounts.AsNoTracking()
            .Where(value => value.Id == id && value.UserId == userId)
            .Include(value => value.Holdings)
            .Include(value => value.Transactions)
            .SingleOrDefaultAsync();
        return account is null ? Results.NotFound() : Results.Ok(ResponseMapper.Account(account));
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var account = new InvestmentAccount
        {
            UserId = principal.GetUserId(),
            Name = request.Name.Trim(),
            AccountType = request.AccountType
        };
        db.InvestmentAccounts.Add(account);
        await db.SaveChangesAsync();
        return Results.Created($"/api/accounts/{account.Id}", ResponseMapper.Account(account));
    }

    private static async Task<IResult> UpdateAccount(Guid id, UpdateAccountRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var account = await db.InvestmentAccounts.Include(value => value.Holdings)
            .Include(value => value.Transactions)
            .SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId);
        if (account is null) return Results.NotFound();
        account.Name = request.Name.Trim();
        account.AccountType = request.AccountType;
        await db.SaveChangesAsync();
        return Results.Ok(ResponseMapper.Account(account));
    }

    private static async Task<IResult> DeleteAccount(Guid id,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var account = await db.InvestmentAccounts
            .SingleOrDefaultAsync(value => value.Id == id && value.UserId == userId);
        if (account is null) return Results.NotFound();
        db.Remove(account);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> CreateHolding(Guid accountId, UpsertHoldingRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        if (!await db.InvestmentAccounts.AnyAsync(value => value.Id == accountId && value.UserId == userId))
            return Results.NotFound();
        var holding = new Holding { InvestmentAccountId = accountId, Symbol = "", AssetName = "" };
        Apply(holding, request);
        db.Holdings.Add(holding);
        await db.SaveChangesAsync();
        return Results.Created($"/api/holdings/{holding.Id}", ResponseMapper.Holding(holding));
    }

    private static async Task<IResult> UpdateHolding(Guid id, UpsertHoldingRequest request,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var holding = await db.Holdings.SingleOrDefaultAsync(value =>
            value.Id == id && value.InvestmentAccount.UserId == userId);
        if (holding is null) return Results.NotFound();
        Apply(holding, request);
        await db.SaveChangesAsync();
        return Results.Ok(ResponseMapper.Holding(holding));
    }

    private static async Task<IResult> DeleteHolding(Guid id,
        System.Security.Claims.ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        var holding = await db.Holdings.SingleOrDefaultAsync(value =>
            value.Id == id && value.InvestmentAccount.UserId == userId);
        if (holding is null) return Results.NotFound();
        db.Remove(holding);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> CreateTransaction(Guid accountId,
        CreateTransactionRequest request, System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userId = principal.GetUserId();
        var accountExists = await db.InvestmentAccounts
            .AnyAsync(value => value.Id == accountId && value.UserId == userId);
        if (!accountExists) return Results.NotFound();
        if (request.HoldingId is not null && !await db.Holdings.AnyAsync(value =>
                value.Id == request.HoldingId && value.InvestmentAccountId == accountId))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.HoldingId)] = ["The holding must belong to the selected account."]
            });
        if (request.TransactionDate == default)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.TransactionDate)] = ["A transaction date is required."]
            });
        var isTrade = request.Type is TransactionType.Buy or TransactionType.Sell;
        if (isTrade && (request.HoldingId is null || request.Quantity <= 0 || request.Price <= 0))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["trade"] = ["Buy and sell records require a holding, positive quantity, and positive price."]
            });
        if (!isTrade && request.Amount <= 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Amount)] = ["Deposits and withdrawals require a positive amount."]
            });

        var transaction = new PortfolioTransaction
        {
            InvestmentAccountId = accountId,
            HoldingId = request.HoldingId,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            Amount = isTrade ? decimal.Round(request.Quantity * request.Price, 2) : request.Amount,
            TransactionDate = request.TransactionDate,
            Note = request.Note?.Trim()
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return Results.Created($"/api/accounts/{accountId}", ResponseMapper.Transaction(transaction));
    }

    private static async Task<IResult> GetSummary(
        System.Security.Claims.ClaimsPrincipal principal, PortfolioService portfolios) =>
        Results.Ok(await portfolios.GetSummaryAsync(principal.GetUserId()));

    private static async Task<IResult> GetAllocation(
        System.Security.Claims.ClaimsPrincipal principal, PortfolioService portfolios)
    {
        var summary = await portfolios.GetSummaryAsync(principal.GetUserId());
        return Results.Ok(summary.Allocation);
    }

    private static void Apply(Holding holding, UpsertHoldingRequest request)
    {
        holding.Symbol = request.Symbol.Trim().ToUpperInvariant();
        holding.AssetName = request.AssetName.Trim();
        holding.AssetClass = request.AssetClass;
        holding.Quantity = request.Quantity;
        holding.AverageCost = request.AverageCost;
        holding.CurrentPrice = request.CurrentPrice;
        holding.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
