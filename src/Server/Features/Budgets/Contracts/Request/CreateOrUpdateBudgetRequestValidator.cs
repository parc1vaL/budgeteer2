using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Budgets.Contracts.Request;

public class CreateOrUpdateBudgetRequestValidator : AbstractValidator<CreateOrUpdateBudgetRequest>
{
    private readonly BudgetContext context;

    public CreateOrUpdateBudgetRequestValidator(BudgetContext context)
    {
        this.context = context;

        RuleFor(v => v.Month)
            .GreaterThanOrEqualTo(1).WithMessage("Month value must be between 1 and 12.")
            .LessThanOrEqualTo(12).WithMessage("Month value must be between 1 and 12.");

        RuleFor(v => v.Year)
            .GreaterThanOrEqualTo(1900).WithMessage("Year value must be between 1900 and 2100.")
            .LessThanOrEqualTo(2100).WithMessage("Year value must be between 1900 and 2100.");

        RuleFor(v => v.CategoryId)
            .NotEmpty().WithMessage("Category ID must be set.")
            .MustAsync(HasValidCategoryId).WithMessage("Category ID is not valid.");
    }

    private async Task<bool> HasValidCategoryId(int id, CancellationToken cancellationToken)
    {
        return await context.Categories.AnyAsync(a => a.Id == id, cancellationToken);
    }
}
