using Budgeteer.Server.Features.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Accounts;

public class AccountService(
    BudgetContext context,
    LinkGenerator linkGenerator,
    IValidator<CreateAccountRequest> createValidator,
    IValidator<UpdateAccountRequest> updateValidator)
{
    public async Task<IResult> GetAccountsAsync(CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await context.Accounts
                .Select(a => new GetAccountResponse
                {
                    Id = a.Id, 
                    Name = a.Name, 
                    OnBudget = a.OnBudget, 
                    Balance = a.Transactions.Sum(t => t.Amount),
                })
                .ToArrayAsync(cancellationToken));
    }

    public async Task<IResult> GetAccountAsync(int id, CancellationToken cancellationToken)
    {
        var result = await context.Accounts
            .Select(a => new GetAccountResponse
            {
                Id = a.Id, 
                Name = a.Name, 
                OnBudget = a.OnBudget, 
                Balance = a.Transactions.Sum(t => t.Amount),
            })
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    public async Task<IResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        await using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var account = new Account { Name = request.Name, OnBudget = request.OnBudget, };

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

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

        context.Transactions.Add(balance);
        await context.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);

        return TypedResults.Created(
            linkGenerator.GetPathByName(Operations.Accounts.GetDetails, new() { ["id"] = account.Id, })
            ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    public async Task<IResult> UpdateAccountAsync(int id, UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (account is null)
        {
            return TypedResults.NotFound();
        }

        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        account.Name = request.Name;

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    public async Task<IResult> DeleteAccountAsync(int id, CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (account is null)
        {
            return TypedResults.NotFound();
        }

        context.Accounts.Remove(account);

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
