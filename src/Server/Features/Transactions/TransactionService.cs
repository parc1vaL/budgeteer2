using Budgeteer.Server.Features.Transactions.Contracts.Request;
using Budgeteer.Server.Features.Transactions.Contracts.Response;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Transactions;

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

    public async Task<IResult> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        var transactions = await this.context.Transactions
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
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(transactions);
    }

    public async Task<IResult> GetTransactionAsync(int id, CancellationToken cancellationToken)
    {
        var result = await this.context.Transactions
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
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    public async Task<IResult> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await this.createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        return request.TransactionType == TransactionType.External
            ? await CreateExternalTransactionAsync(request, cancellationToken)
            : await CreateInternalTransactionAsync(request, cancellationToken);        
    }

    private async Task<IResult> CreateExternalTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
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
        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            this.linkGenerator.GetPathByName(Operations.Transactions.GetDetails, new RouteValueDictionary { ["id"] = transaction.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    private async Task<IResult> CreateInternalTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction creation request has transaction type "
                + "'Transfer' but no transaction account ID set.");
        }

        var accountOnBudget = await this.context.Accounts
            .Where(a => a.Id == request.AccountId)
            .Select(a => a.OnBudget)
            .FirstOrDefaultAsync(cancellationToken);

        await using var dbTransaction = await this.context.Database.BeginTransactionAsync(cancellationToken);

        var transaction = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = request.IsCleared,
            AccountId = request.AccountId,
            CategoryId = accountOnBudget ? request.CategoryId : default,
            TransferAccountId = request.TransferAccountId,
            Date = request.Date,
            Amount = request.Amount,
            TransferTransactionId = null, // added later on
        };

        this.context.Transactions.Add(transaction);
        await this.context.SaveChangesAsync(cancellationToken);

        var transfer = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = false,
            AccountId = request.TransferAccountId.Value,
            CategoryId = accountOnBudget ? default : request.CategoryId,
            TransferAccountId = request.AccountId,
            Date = request.Date,
            Amount = -1.0M * request.Amount,
            TransferTransactionId = transaction.Id,
        };

        this.context.Transactions.Add(transfer);
        await this.context.SaveChangesAsync(cancellationToken);

        transaction.TransferTransactionId = transfer.Id;
        await this.context.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);

        return TypedResults.Created(
            this.linkGenerator.GetPathByName(Operations.Transactions.GetDetails, new() { ["id"] = transaction.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    public async Task<IResult> UpdateTransactionAsync(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await this.context.Transactions.FindAsync(new object[] { id, }, cancellationToken);

        if (transaction is null)
        {
            return TypedResults.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        if (transaction.TransactionType == TransactionType.Internal)
        {
            if (!transaction.TransferTransactionId.HasValue)
            {
                throw new Exception(
                    $"Transaction with ID {id} has transaction type "
                    + "'Transfer' but not transfer transaction ID set.");
            }

            var transfer = await this.context.Transactions.FindAsync(new object[] { transaction.TransferTransactionId.Value, }, cancellationToken);

            if (transfer is null)
            {
                throw new Exception(
                    $"Transaction with ID {id} has transaction type 'Transfer' "
                    + "but the transfer transaction with ID "
                    + $"{transaction.TransferTransactionId.Value} does not exist.");
            }

            if (request.TransactionType == TransactionType.Internal)
            {
                return await UpdateInternalToInternalAsync(transaction, transfer, request, cancellationToken);
            }
            else
            {
                return await UpdateInternalToExternalAsync(transaction, transfer, request, cancellationToken);
            }
        }
        else
        {
            if (request.TransactionType == TransactionType.Internal)
            {
                return await UpdateExternalToInternalAsync(transaction, request, cancellationToken);
            }
            else
            {
                return await UpdateExternalToExternalAsync(transaction, request, cancellationToken);
            }
        }
    }

    private async Task<IResult> UpdateInternalToInternalAsync(
        Transaction transaction, 
        Transaction transfer, 
        UpdateTransactionRequest request, 
        CancellationToken cancellationToken)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction update request has transaction type "
                + "'Transfer' but no transaction account ID set.",
                nameof(request));
        }

        var accountOnBudget = await this.context.Accounts
            .Where(a => a.Id == request.AccountId)
            .Select(a => a.OnBudget)
            .FirstOrDefaultAsync(cancellationToken);

        // unchanged:
        transaction.TransactionType = TransactionType.Internal;
        transaction.TransferTransactionId = transaction.TransferTransactionId;
        transaction.IncomeType = IncomeType.None;
        transaction.Payee = null;

        transfer.TransactionType = TransactionType.Internal;
        transfer.TransferTransactionId = transfer.TransferTransactionId;
        transfer.IncomeType = IncomeType.None;
        transfer.Payee = null;
        transfer.IsCleared = transfer.IsCleared;

        // potentially changing:
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = accountOnBudget ? request.CategoryId : default;
        transaction.TransferAccountId = request.TransferAccountId.Value;
        transaction.Date = request.Date;
        transaction.Amount = request.Amount;
        transaction.IsCleared = request.IsCleared;

        transfer.AccountId = request.TransferAccountId.Value;
        transfer.CategoryId = accountOnBudget ? default : request.CategoryId;
        transfer.TransferAccountId = request.AccountId;
        transfer.Date = request.Date;
        transfer.Amount = -1.0M * request.Amount;

        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    private async Task<IResult> UpdateInternalToExternalAsync(
        Transaction transaction, 
        Transaction transfer, 
        UpdateTransactionRequest request, 
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await this.context.Database.BeginTransactionAsync(cancellationToken);

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

        await this.context.SaveChangesAsync(cancellationToken);

        this.context.Transactions.Remove(transfer);
        await this.context.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);

        return TypedResults.Ok();
    }

    private async Task<IResult> UpdateExternalToInternalAsync(
        Transaction transaction, 
        UpdateTransactionRequest request, 
        CancellationToken cancellationToken)
    {
        if (!request.TransferAccountId.HasValue)
        {
            throw new ArgumentException(
                "Transaction update request has transaction type "
                + "'Transfer' but no transaction account ID set.",
                nameof(request));
        }

        var accountOnBudget = await this.context.Accounts
            .Where(a => a.Id == request.AccountId)
            .Select(a => a.OnBudget)
            .FirstOrDefaultAsync(cancellationToken);

        await using var dbTransaction = await this.context.Database.BeginTransactionAsync(cancellationToken);

        // create new transfer
        var transfer = new Transaction
        {
            TransactionType = TransactionType.Internal,
            IncomeType = IncomeType.None,
            Payee = null,
            IsCleared = false,
            AccountId = request.TransferAccountId.Value,
            CategoryId = accountOnBudget ? default : request.CategoryId,
            TransferAccountId = request.AccountId,
            Date = request.Date,
            Amount = -1.0M * request.Amount,
            TransferTransactionId = transaction.Id,
        };

        // unchanged in original transaction
        transaction.TransactionType = TransactionType.Internal;
        transaction.TransferTransactionId = transaction.TransferTransactionId;
        transaction.IncomeType = IncomeType.None;
        transaction.Payee = null;

        // adjust original transaction
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = accountOnBudget ? request.CategoryId : default;
        transaction.TransferAccountId = request.TransferAccountId.Value;
        transaction.Date = request.Date;
        transaction.Amount = request.Amount;
        transaction.IsCleared = request.IsCleared;

        this.context.Transactions.Add(transfer);
        await this.context.SaveChangesAsync(cancellationToken);

        transaction.TransferTransactionId = transfer.Id;

        await this.context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return TypedResults.Ok();
    }

    private async Task<IResult> UpdateExternalToExternalAsync(
        Transaction transaction, 
        UpdateTransactionRequest request, 
        CancellationToken cancellationToken)
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

        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    public async Task<IResult> DeleteTransactionAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await this.context.Transactions.FindAsync(new object[] { id, }, cancellationToken);

        if (transaction is null)
        {
            return TypedResults.NotFound();
        }

        this.context.Transactions.Remove(transaction);

        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
