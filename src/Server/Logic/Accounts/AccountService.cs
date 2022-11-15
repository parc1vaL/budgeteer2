using Budgeteer.Server.Entities;
using Budgeteer.Shared;
using Budgeteer.Shared.Accounts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Logic.Accounts;

public class AccountService
{
    private readonly BudgetContext context;
    private readonly LinkGenerator linkGenerator;
    private readonly IValidator<CreateAccountRequest> createValidator;
    private readonly IValidator<UpdateAccountRequest> updateValidator;

    public AccountService(
        BudgetContext context,
        LinkGenerator linkGenerator,
        IValidator<CreateAccountRequest> createValidator,
        IValidator<UpdateAccountRequest> updateValidator)
    {
        this.context = context;
        this.linkGenerator = linkGenerator;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    public async Task<IResult> GetAccountsAsync()
    {
        return Results.Ok(
            await this.context.Accounts
                .AsNoTracking()
                .Select(a => new AccountListItem
                {
                    Id = a.Id,
                    Name = a.Name,
                    OnBudget = a.OnBudget,
                    Balance = a.Transactions.Sum(t => t.Amount),
                })
                .ToArrayAsync());
    }

    public async Task<IResult> GetAccountAsync(int id)
    {
        var result = await this.context.Accounts
            .AsNoTracking()
            .Select(a => new AccountDetails 
            { 
                Id = a.Id, 
                Name = a.Name, 
                OnBudget = a.OnBudget,
                Balance = a.Transactions.Sum(t => t.Amount),
            })
            .FirstOrDefaultAsync(a => a.Id == id);

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound();
    }

    public async Task<IResult> CreateAccountAsync(CreateAccountRequest request)
    {
        var validationResult = await this.createValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        var account = new Account
        {
            Name = request.Name,
            OnBudget = request.OnBudget,
        };

        this.context.Accounts.Add(account);
        await this.context.SaveChangesAsync();

        var balance = new Transaction
        {
            AccountId = account.Id,
            Amount = request.Balance,
            CategoryId = null,
            Date = DateOnly.FromDateTime(DateTime.Today),
            IncomeType = IncomeType.CurrentMonth,
            IsCleared = true,
            Payee = "Initial balance",
            TransactionType = TransactionType.External,
            TransferAccountId = null,
            TransferTransactionId = null,
        };

        this.context.Transactions.Add(balance);
        await this.context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        return Results.Created(
            this.linkGenerator.GetPathByName(Operations.Accounts.GetDetails, new() { ["id"] = account.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."),
            account);
    }

    public async Task<IResult> UpdateAccountAsync(int id, UpdateAccountRequest request)
    {
        var account = await this.context.Accounts.FindAsync(id);

        if (account is null)
        {
            return Results.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        account.Name = request.Name;

        await this.context.SaveChangesAsync();

        return Results.Ok(account);
    }

    public async Task<IResult> DeleteAccountAsync(int id)
    {
        var account = await this.context.Accounts.FindAsync(id);

        if (account is null)
        {
            return Results.NotFound();
        }

        this.context.Accounts.Remove(account);

        await this.context.SaveChangesAsync();

        return Results.Ok();
    }
}