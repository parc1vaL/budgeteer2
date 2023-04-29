using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Transactions;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionRequest>
{
    private readonly BudgetContext context;

    public CreateTransactionCommandValidator(BudgetContext context)
    {
        this.context = context;

        RuleFor(r => r.TransactionType)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(r => r.IncomeType)
            .IsInEnum().WithMessage("Invalid income type.")
            .Equal(IncomeType.None)
                .When(r => r.TransactionType == TransactionType.Internal, ApplyConditionTo.CurrentValidator)
                .WithMessage($"Income type must be {nameof(IncomeType.None)} for internal transfers.");

        RuleFor(r => r.AccountId)
            .NotEmpty().WithMessage("Account ID must be set.")
            .MustAsync(HasValidAccountId).WithMessage("Account ID is invalid.");

        RuleFor(r => r.Date)
            .NotEmpty().WithMessage("Transaction date must be set.");

        When(r => r.TransactionType == TransactionType.External, () =>
        {
            RuleFor(r => r.TransferAccountId)
                .Null().WithMessage("Non-transfer transactions must not have a transaction account.");
            
            RuleFor(r => r.Payee)
                .NotEmpty().WithMessage("Transaction payee must be set for non-transfer transactions.")
                .MaximumLength(200).WithMessage("Transaction payee cannot exceed 200 characters.");

            WhenAsync(IsAccountOffBudget, () =>
            {
                RuleFor(r => r.CategoryId)
                    .Null().WithMessage("Category must not be set for off-budget transactions.");
            }).Otherwise(() =>
            {
                RuleFor(r => r.CategoryId)
                    .Null()
                        .When(r => r.IncomeType != IncomeType.None, ApplyConditionTo.CurrentValidator)
                        .WithMessage("Category must not be set for income transactions.")
                    .NotEmpty()
                        .When(r => r.IncomeType == IncomeType.None, ApplyConditionTo.CurrentValidator)
                        .WithMessage("Category must be set for non-income transactions.")
                    .MustAsync(HasValidCategoryId)
                        .When(r => r.IncomeType == IncomeType.None, ApplyConditionTo.CurrentValidator)
                        .WithMessage("Category ID is invalid.");
            });
        });

        When(r => r.TransactionType == TransactionType.Internal, () =>
        {
            RuleFor(r => r.TransferAccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Transfer account ID must be set for transfer transactions.")
                .NotEqual(r => r.AccountId).WithMessage("Account ID and transfer account ID must not be identical.")
                .MustAsync((t, ct) => HasValidAccountId(t!.Value, ct)).WithMessage("Transfer account ID is invalid.");

            RuleFor(r => r.Payee)
                .Null().WithMessage("Transfer transactions must not have a payee.");

            WhenAsync(IsAccountOffBudget, () =>
            {
                WhenAsync(IsTransferAccountOffBudget, () =>
                {
                    RuleFor(r => r.CategoryId)
                        .Null().WithMessage("Category must not be set for transfers between off-budget accounts.");
                }).Otherwise(() =>
                {
                    RuleFor(r => r.CategoryId)
                        .NotEmpty().WithMessage("Category must be set for non-income transfers from on-budget to off-budget account.")
                        .MustAsync(HasValidCategoryId).WithMessage("Category ID is invalid.");
                });
            }).Otherwise(() =>
            {
                WhenAsync(IsTransferAccountOffBudget, () =>
                {
                    RuleFor(r => r.CategoryId)
                        .NotEmpty().WithMessage("Category must be set for non-income transfers from off-budget to on-budget account.")
                        .MustAsync(HasValidCategoryId).WithMessage("Category ID is invalid.");
                }).Otherwise(() =>
                {
                    RuleFor(r => r.CategoryId)
                        .Null().WithMessage("Category must not be set for transfers between on-budget accounts.");
                });
            });
        });
    }

    private async Task<bool> IsAccountOffBudget(CreateTransactionRequest command, CancellationToken cancellationToken)
    {
        return !(await context.Accounts
            .Select(a => new { a.Id, a.OnBudget, })
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken))?.OnBudget ?? false;
    }

    private async Task<bool> IsTransferAccountOffBudget(CreateTransactionRequest command, CancellationToken cancellationToken)
    {
        return !(await context.Accounts
            .Select(a => new { a.Id, a.OnBudget, })
            .FirstOrDefaultAsync(a => a.Id == command.TransferAccountId, cancellationToken))?.OnBudget ?? false;
    }

    private async Task<bool> HasValidAccountId(int id, CancellationToken cancellationToken)
    {
        return await context.Accounts.AnyAsync(a => a.Id == id, cancellationToken);
    }

    private async Task<bool> HasValidCategoryId(int? id, CancellationToken cancellationToken)
    {
        return await context.Categories.AnyAsync(a => a.Id == id, cancellationToken);
    }
}
