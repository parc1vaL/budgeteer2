using Budgeteer.Shared;
using Budgeteer.Shared.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Logic.Transactions;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionRequest>
{
    private readonly BudgetContext context;

    public CreateTransactionCommandValidator(BudgetContext context)
    {
        this.context = context;

        RuleFor(r => r.TransactionType)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(r => r.AccountId)
            .NotEmpty().WithMessage("Account ID must not be zero.")
            .MustAsync(HasValidAccountId).WithMessage("Account ID is invalid.");

        RuleFor(r => r.Date)
            .NotEmpty().WithMessage("Transaction date must be set.");

        RuleFor(r => r.Payee)
            .Null()
                .When(r => r.TransactionType == TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("Transfer transactions must not have a payee.")
            .NotEmpty()
                .When(r => r.TransactionType != TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("Transaction payee must be set for non-transfer transactions.")
            .MaximumLength(200)
                .WithMessage("Transaction payee cannot exceed 200 characters.");

        RuleFor(r => r.IncomeType)
            .IsInEnum().WithMessage("Invalid income type.")
            .Equal(IncomeType.None)
                .When(r => r.TransactionType == TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage($"Income type must be {nameof(IncomeType.None)} for transfer transactions.");

        RuleFor(r => r.TransactionAccountId)
            .Null()
                .When(r => r.TransactionType != TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("Non-transfer transactions must not have a transaction account.")
            .NotEmpty()
                .When(r => r.TransactionType == TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("Transfer account ID must be set for transfer transactions.")
            .NotEqual(r => r.AccountId)
                .When(r => r.TransactionType == TransactionType.Transfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("Account ID and transfer account ID must not be identical.")
            .MustAsync((t, ct) => HasValidAccountId(t!.Value, ct))
                .When(r => r.TransactionType == TransactionType.Transfer && r.TransactionAccountId.HasValue, ApplyConditionTo.CurrentValidator)
                .WithMessage("Transfer account ID is invalid.");

        WhenAsync(IsOffBudgetTransaction, () =>
        {
            // RuleFor(t => t.CategoryId)
            //     .Null().WithMessage("Category ID cannot be set for off-budget transactions.");

            RuleFor(t => t.IncomeType)
                .Equal(IncomeType.None).WithMessage(
                    $"Income type must be {nameof(IncomeType.None)} for off-budget transactions");
        }).Otherwise(() =>
        {
            When(r => r.TransactionType == TransactionType.Expense, () =>
            {
                // RuleFor(t => t.CategoryId)
                //     .NotEmpty().WithMessage("Category ID must not be zero for expense transactions")
                //     .MustAsync(HasValidCategoryId).WithMessage("Category ID is invalid.");

                RuleFor(t => t.IncomeType)
                    .Equal(IncomeType.None).WithMessage($"Income type must be {nameof(IncomeType.None)} for expense transactions.");
            });

            When(r => r.TransactionType == TransactionType.Income, () =>
            {
                // RuleFor(t => t.CategoryId)
                //     .Null().WithMessage("Category ID cannot be set for income transactions.");

                RuleFor(t => t.IncomeType)
                    .NotEqual(IncomeType.None).WithMessage(
                        $"Income type must be {nameof(IncomeType.CurrentMonth)} or " +
                        $"{nameof(IncomeType.NextMonth)} for on-budget income transactions.");
            });
        });
    }

    private async Task<bool> IsOffBudgetTransaction(CreateTransactionRequest command, CancellationToken cancellationToken)
    {
        return !(await context.Accounts
            .Select(a => new { a.Id, a.OnBudget, })
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken))?.OnBudget ?? false;
    }

    private async Task<bool> HasValidAccountId(int id, CancellationToken cancellationToken)
    {
        return await context.Accounts
            .AnyAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    // private async Task<bool> HasValidCategoryId(int? id, CancellationToken cancellationToken)
    // {
    //     return await context.Categories
    //         .AnyAsync(a => a.Id == id, cancellationToken)
    //         .ConfigureAwait(false);
    // }
}
