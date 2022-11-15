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
                Category = t.Category!.Name,
                CategoryId = t.CategoryId,
                Date = t.Date,
                Payee = t.Payee,
                IncomeType = t.IncomeType,
                Amount = t.Amount,
            })
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Id)
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
                Category = t.Category!.Name,
                CategoryId = t.CategoryId,
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

        return request.TransactionType == TransactionType.External
            ? await CreateExternalTransactionAsync(request)
            : await CreateInternalTransactionAsync(request);        
    }

    private async Task<IResult> CreateExternalTransactionAsync(CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            TransactionType = TransactionType.External,
            IsCleared = request.IsCleared,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Payee = request.Payee,
            IncomeType = request.IncomeType,
            Amount = request.Amount,
            TransferAccountId = null,
            TransferTransactionId = null,
        };

        this.context.Transactions.Add(transaction);
        await this.context.SaveChangesAsync();

        return Results.Created(
            this.linkGenerator.GetPathByName(Operations.Transactions.GetDetails, new() { ["id"] = transaction.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."),
            transaction);
    }

    private async Task<IResult> CreateInternalTransactionAsync(CreateTransactionRequest request)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction creation request has transaction type "
                + "'Transfer' but no transaction account ID set.");
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        var transaction = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = request.IsCleared,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            TransferAccountId = request.TransferAccountId,
            Date = request.Date,
            Amount = request.Amount,
            TransferTransactionId = null, // added later on
        };

        this.context.Transactions.Add(transaction);
        await this.context.SaveChangesAsync();

        var transfer = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = false,
            AccountId = request.TransferAccountId.Value,
            CategoryId = request.CategoryId,
            TransferAccountId = request.AccountId,
            Date = request.Date,
            Amount = -1.0M * request.Amount,
            TransferTransactionId = transaction.Id,
        };

        this.context.Transactions.Add(transfer);
        await this.context.SaveChangesAsync();

        transaction.TransferTransactionId = transfer.Id;
        await this.context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        return Results.Created(
            this.linkGenerator.GetPathByName(Operations.Transactions.GetDetails, new() { ["id"] = transaction.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."),
            transaction);
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

        if (transaction.TransactionType == TransactionType.Internal)
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

            if (request.TransactionType == TransactionType.Internal)
            {
                return await UpdateInternalToInternalAsync(transaction, transfer, request);
            }
            else
            {
                return await UpdateInternalToExternalAsync(transaction, transfer, request);
            }
        }
        else
        {
            if (request.TransactionType == TransactionType.Internal)
            {
                return await UpdateExternalToInternalAsync(transaction, request);
            }
            else
            {
                return await UpdateExternalToExternalAsync(transaction, request);
            }
        }
    }

    private async Task<IResult> UpdateInternalToInternalAsync(
        Transaction transaction, 
        Transaction transfer, 
        UpdateTransactionRequest request)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction update request has transaction type "
                + "'Transfer' but no transaction account ID set.",
                nameof(request));
        }

        // unchanged:
        transaction.TransactionType = TransactionType.Internal;
        transaction.TransferTransactionId = transaction.TransferTransactionId;
        transaction.IncomeType = IncomeType.None;
        transaction.CategoryId = null;
        transaction.Payee = null;

        transfer.TransactionType = TransactionType.Internal;
        transfer.TransferTransactionId = transfer.TransferTransactionId;
        transfer.IncomeType = IncomeType.None;
        transfer.CategoryId = null;
        transfer.Payee = null;
        transfer.IsCleared = transfer.IsCleared;

        // potentially changing:
        transaction.AccountId = request.AccountId;
        transaction.TransferAccountId = request.TransferAccountId.Value;
        transaction.Date = request.Date;
        transaction.Amount = request.Amount;
        transaction.IsCleared = request.IsCleared;

        transfer.AccountId = request.TransferAccountId.Value;
        transfer.TransferAccountId = request.AccountId;
        transfer.Date = request.Date;
        transfer.Amount = -1.0M * request.Amount;

        await this.context.SaveChangesAsync();

        return Results.Ok();
    }

    private async Task<IResult> UpdateInternalToExternalAsync(
        Transaction transaction, 
        Transaction transfer, 
        UpdateTransactionRequest request)
    {
        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        // reset transfer properties
        transaction.TransactionType = TransactionType.External;
        transaction.TransferTransactionId = null;
        transaction.TransferAccountId = null;

        // adjust transaction details
        transaction.IncomeType = request.IncomeType;
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.IsCleared = request.IsCleared;
        transaction.Payee = request.Payee;

        await this.context.SaveChangesAsync();

        this.context.Transactions.Remove(transfer);
        await this.context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        return Results.Ok();
    }

    private async Task<IResult> UpdateExternalToInternalAsync(
        Transaction transaction, 
        UpdateTransactionRequest request)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction update request has transaction type "
                + "'Transfer' but no transaction account ID set.",
                nameof(request));
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        // create new transfer
        var transfer = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = false,
            AccountId = request.TransferAccountId.Value,
            CategoryId = request.CategoryId,
            TransferAccountId = request.AccountId,
            Date = request.Date,
            Amount = -1.0M * request.Amount,
            TransferTransactionId = transaction.Id,
        };

        // unchanged in original transaction
        transaction.TransactionType = TransactionType.Internal;
        transaction.TransferTransactionId = transaction.TransferTransactionId;
        transaction.IncomeType = IncomeType.None;
        transaction.CategoryId = null;
        transaction.Payee = null;

        // adjust original transaction
        transaction.AccountId = request.AccountId;
        transaction.TransferAccountId = request.TransferAccountId.Value;
        transaction.Date = request.Date;
        transaction.Amount = request.Amount;
        transaction.IsCleared = request.IsCleared;

        this.context.Transactions.Add(transfer);
        await this.context.SaveChangesAsync();

        transaction.TransferTransactionId = transfer.Id;

        await this.context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return Results.Ok();
    }

    private async Task<IResult> UpdateExternalToExternalAsync(
        Transaction transaction, 
        UpdateTransactionRequest request)
    {
        // ensure unset transfer properties
        transaction.TransactionType = TransactionType.External;
        transaction.TransferTransactionId = null;
        transaction.TransferAccountId = null;

        // adjust transaction details
        transaction.IncomeType = request.IncomeType;
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.IsCleared = request.IsCleared;
        transaction.Payee = request.Payee;

        await this.context.SaveChangesAsync();

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