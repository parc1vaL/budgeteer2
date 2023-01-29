using Budgeteer.Server.Features.Accounts.Contracts.Request;
using Budgeteer.Server.Features.Accounts.Contracts.Response;
using Budgeteer.Server.Features.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Accounts;

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

    public async Task<IResult> GetAccountsAsync(CancellationToken cancellationToken)
    {
        return Results.Ok(
            await this.context.Accounts
                .Select(a => new AccountListItem
                {
                    Id = a.Id, Name = a.Name, OnBudget = a.OnBudget, Balance = a.Transactions.Sum(t => t.Amount),
                })
                .ToArrayAsync(cancellationToken));
    }

    public async Task<IResult> GetAccountAsync(int id, CancellationToken cancellationToken)
    {
        var result = await this.context.Accounts
            .Select(a => new AccountDetails
            {
                Id = a.Id, Name = a.Name, OnBudget = a.OnBudget, Balance = a.Transactions.Sum(t => t.Amount),
            })
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound();
    }

    public async Task<IResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await this.createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        await using var dbTransaction = await this.context.Database.BeginTransactionAsync(cancellationToken);

        var account = new Account { Name = request.Name, OnBudget = request.OnBudget, };

        this.context.Accounts.Add(account);
        await this.context.SaveChangesAsync(cancellationToken);

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
        await this.context.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);

        return TypedResults.Created(
            this.linkGenerator.GetPathByName(Operations.Accounts.GetDetails, new() { ["id"] = account.Id, })
            ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    public async Task<IResult> UpdateAccountAsync(int id, UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await this.context.Accounts.FindAsync(new object[] { id, }, cancellationToken);

        if (account is null)
        {
            return Results.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        account.Name = request.Name;

        await this.context.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    public async Task<IResult> DeleteAccountAsync(int id, CancellationToken cancellationToken)
    {
        var account = await this.context.Accounts.FindAsync(new object[] { id, }, cancellationToken);

        if (account is null)
        {
            return Results.NotFound();
        }

        this.context.Accounts.Remove(account);

        await this.context.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
