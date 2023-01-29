using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Accounts.Contracts.Request;

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    private readonly BudgetContext context;

    public UpdateAccountRequestValidator(BudgetContext context)
    {
        this.context = context;

        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(200).WithMessage("Account name cannot exceed 200 characters.")
            .MustAsync(HasUniqueName).WithMessage("Account name already exists.");
    }

    private async Task<bool> HasUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await context.Accounts
            .AnyAsync(a => a.Name == name, cancellationToken)
            .ConfigureAwait(false);
    }
}