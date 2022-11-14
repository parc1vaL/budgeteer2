using Budgeteer.Server.Entities;
using Budgeteer.Shared;
using Budgeteer.Shared.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Logic.Transactions;

public class TransactionService
{
    private readonly BudgetContext context;
    private readonly LinkGenerator linkGenerator;
    private readonly IValidator<CreateTransactionRequest> createValidator;
    private readonly IValidator<UpdateTransactionRequest> updateValidator;

    public TransactionService(
        BudgetContext context,
        LinkGenerator linkGenerator,
        IValidator<CreateTransactionRequest> createValidator,
        IValidator<UpdateTransactionRequest> updateValidator)
    {
        this.context = context;
        this.linkGenerator = linkGenerator;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    public async Task<TransactionListItem[]> GetTransactionsAsync()
    {
        return await this.context.Transactions
            .AsNoTracking()
            .Select(t => new TransactionListItem
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                IsCleared = t.IsCleared,
                Account = t.Account.Name,
                AccountId = t.AccountId,
                Date = t.Date,
                Payee = t.Payee,
                IncomeType = t.IncomeType,
                Amount = t.Amount,
            })
            .ToArrayAsync();
    }

    public async Task<IResult> GetTransactionAsync(int id)
    {
        var result = await this.context.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TransactionDetails 
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                IsCleared = t.IsCleared,
                Account = t.Account.Name,
                AccountId = t.AccountId,
                Date = t.Date,
                Payee = t.Payee,
                IncomeType = t.IncomeType,
                Amount = t.Amount,
            })
            .FirstOrDefaultAsync();

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound();
    }

    public async Task<IResult> CreateTransactionAsync(CreateTransactionRequest request)
    {
        var validationResult = await this.createValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        var transaction = new Transaction
        {
            TransactionType = request.TransactionType,
            IsCleared = request.IsCleared,
            AccountId = request.AccountId,
            TransferAccountId = request.TransactionAccountId,
            Date = request.Date,
            Payee = request.Payee,
            IncomeType = request.IncomeType,
            Amount = request.Amount,
            TransferTransactionId = null,
        };

        this.context.Transactions.Add(transaction);
        await this.context.SaveChangesAsync();

        if (request.TransactionType == TransactionType.Transfer)
        {
            if (!request.TransactionAccountId.HasValue)
            {
                throw new Exception(
                    "Transaction creation request has transaction type "
                    + "'Transfer' but no transaction account ID set.");
            }

            var transfer = new Transaction
            {
                TransactionType = TransactionType.Transfer,
                IsCleared = request.IsCleared,
                AccountId = request.TransactionAccountId.Value,
                TransferAccountId = request.AccountId,
                Date = request.Date,
                Payee = request.Payee,
                IncomeType = IncomeType.None,
                Amount = -1.0M * request.Amount,
                TransferTransactionId = transaction.Id,
            };

            this.context.Transactions.Add(transfer);
            await this.context.SaveChangesAsync();

            transaction.TransferTransactionId = transfer.Id;
            await this.context.SaveChangesAsync();
        }

        await dbTransaction.CommitAsync();

        return Results.Created(
            this.linkGenerator.GetPathByName(Operations.Transactions.GetDetails, new() { ["id"] = transaction.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."),
            null);
    }

    public async Task<IResult> UpdateTransactionAsync(int id, UpdateTransactionRequest request)
    {
        var transaction = await this.context.Transactions.FindAsync(id);

        if (transaction is null)
        {
            return Results.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        if (transaction.TransactionType == TransactionType.Transfer)
        {
            if (!transaction.TransferTransactionId.HasValue)
            {
                throw new Exception(
                    $"Transaction with ID {id} has transaction type "
                    + "'Transfer' but not transfer transaction ID set.");
            }

            var transfer = await this.context.Transactions.FindAsync(transaction.TransferTransactionId.Value);

            if (transfer is null)
            {
                throw new Exception(
                    $"Transaction with ID {id} has transaction type 'Transfer' "
                    + "but the transfer transaction with ID "
                    + $"{transaction.TransferTransactionId.Value} does not exist.");
            }

            if (request.TransactionType == TransactionType.Transfer)
            {
                if (!request.TransactionAccountId.HasValue)
                {
                    throw new Exception(
                        "Transaction update request has transaction type "
                        + "'Transfer' but no transaction account ID set.");
                }

                transfer.AccountId = request.TransactionAccountId.Value;
                transfer.TransferAccountId = request.AccountId;
                transfer.Date = request.Date;
                transfer.Payee = request.Payee;
                transfer.IncomeType = IncomeType.None;
                transfer.Amount = -1.0M * request.Amount;
                transfer.IsCleared = request.IsCleared;
            }
            else 
            {
                transaction.TransferTransactionId = null;
                this.context.Transactions.Remove(transfer);
            }
        }
        else if (request.TransactionType == TransactionType.Transfer)
        {
            if (!request.TransactionAccountId.HasValue)
            {
                throw new Exception(
                    "Transaction creation request has transaction type "
                    + "'Transfer' but no transaction account ID set.");
            }

            var transfer = new Transaction
            {
                TransactionType = TransactionType.Transfer,
                IsCleared = request.IsCleared,
                AccountId = request.TransactionAccountId.Value,
                TransferAccountId = request.AccountId,
                Date = request.Date,
                Payee = request.Payee,
                IncomeType = IncomeType.None,
                Amount = -1.0M * request.Amount,
                TransferTransactionId = transaction.Id,
            };

            this.context.Transactions.Add(transfer);
            await this.context.SaveChangesAsync();

            transaction.TransferTransactionId = transfer.Id;
        }

        transaction.TransactionType = request.TransactionType;
        transaction.AccountId = request.AccountId;
        transaction.TransferAccountId = request.TransactionAccountId;
        transaction.Date = request.Date;
        transaction.Payee = request.Payee;
        transaction.IncomeType = request.IncomeType;
        transaction.Amount = request.Amount;
        transaction.IsCleared = request.IsCleared;

        await this.context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return Results.Ok();
    }

    public async Task<IResult> DeleteTransactionAsync(int id)
    {
        var transaction = await this.context.Transactions.FindAsync(id);

        if (transaction is null)
        {
            return Results.NotFound();
        }

        this.context.Transactions.Remove(transaction);

        await this.context.SaveChangesAsync();

        return Results.Ok();
    }
}