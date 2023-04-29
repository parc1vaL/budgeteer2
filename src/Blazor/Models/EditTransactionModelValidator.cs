using FluentValidation;

namespace Budgeteer.Blazor.Models;

public class EditTransactionModelValidator : AbstractValidator<EditTransactionModel>
{
    private readonly Dictionary<int, GetAccountResponse> accounts;
    private readonly Dictionary<int, GetCategoriesResponse> categories;

    public EditTransactionModelValidator(
        IEnumerable<GetAccountResponse> accounts,
        IEnumerable<GetCategoriesResponse> categories)
    {
        this.accounts = accounts.ToDictionary(account => account.Id);
        this.categories = categories.ToDictionary(category => category.Id);

        RuleFor(r => r.TransactionType)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(r => r.IncomeType)
            .IsInEnum().WithMessage("Invalid income type.");

        RuleFor(r => r.Account)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Account must be set.")
            .Must(HasValidAccount).WithMessage("Account is invalid.");

        RuleFor(r => r.Date)
            .NotEmpty().WithMessage("Transaction date must be set.");

        When(r => r.TransactionType == TransactionType.External, () =>
        {
            RuleFor(r => r.Payee)
                .NotEmpty().WithMessage("Transaction payee must be set for non-transfer transactions.")
                .MaximumLength(200).WithMessage("Transaction payee cannot exceed 200 characters.");

            When(IsAccountOnBudget, () =>
            {
                RuleFor(r => r.Category)
                    .Cascade(CascadeMode.Stop)
                    .Null()
                    .When(r => r.IncomeType != IncomeType.None, ApplyConditionTo.CurrentValidator)
                    .WithMessage("Category must not be set for income transactions.")
                    .NotEmpty()
                    .When(r => r.IncomeType == IncomeType.None, ApplyConditionTo.CurrentValidator)
                    .WithMessage("Category must be set for non-income transactions.")
                    .Must(HasValidCategory)
                    .When(r => r.IncomeType == IncomeType.None, ApplyConditionTo.CurrentValidator)
                    .WithMessage("Category is invalid.");
            });
        });

        When(r => r.TransactionType == TransactionType.Internal, () =>
        {
            RuleFor(r => r.TransferAccount)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Transfer account must be set for transfer transactions.")
                .NotEqual(r => r.Account).WithMessage("Account and transfer account must not be identical.")
                .Must(HasValidAccount).WithMessage("Transfer account is invalid.");

            When(IsAccountOffBudget, () =>
            {
                When(IsTransferAccountOnBudget, () =>
                {
                    RuleFor(r => r.Category)
                        .Cascade(CascadeMode.Stop)
                        .NotEmpty().WithMessage("Category must be set for non-income transfers from on-budget to off-budget account.")
                        .Must(HasValidCategory).WithMessage("Category is invalid.");
                });
            }).Otherwise(() =>
            {
                When(IsTransferAccountOffBudget, () =>
                {
                    RuleFor(r => r.Category)
                        .Cascade(CascadeMode.Stop)
                        .NotEmpty().WithMessage("Category must be set for non-income transfers from off-budget to on-budget account.")
                        .Must(HasValidCategory).WithMessage("Category is invalid.");
                });
            });
        });
    }

    private bool IsAccountOnBudget(EditTransactionModel command)
    {
        return command.Account is not null 
               && this.accounts.TryGetValue(command.Account.Id, out var account) 
               && account.OnBudget;
    }

    private bool IsAccountOffBudget(EditTransactionModel command)
    {
        return command.Account is not null
            && this.accounts.TryGetValue(command.Account.Id, out var account) 
            && !account.OnBudget;
    }

    private bool IsTransferAccountOnBudget(EditTransactionModel command)
    {
        return command.TransferAccount is not null 
               && this.accounts.TryGetValue(command.TransferAccount.Id, out var account) 
               && account.OnBudget;
    }

    private bool IsTransferAccountOffBudget(EditTransactionModel command)
    {
        return command.TransferAccount is not null
               && this.accounts.TryGetValue(command.TransferAccount.Id, out var account) 
               && !account.OnBudget;
    }

    private bool HasValidAccount(GetAccountResponse? account)
    {
        return account is not null && this.accounts.ContainsKey(account.Id);
    }

    private bool HasValidCategory(GetCategoriesResponse? category)
    {
        return category is not null && this.categories.ContainsKey(category.Id);
    }
}
