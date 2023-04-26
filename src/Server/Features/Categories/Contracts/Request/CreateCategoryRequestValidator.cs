using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Categories.Contracts.Request;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    private readonly BudgetContext context;

    public CreateCategoryRequestValidator(BudgetContext context)
    {
        this.context = context;

        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Category name cannot exceed 200 characters.")
            .MustAsync(HasUniqueName).WithMessage("Category name already exists.");
    }

    private async Task<bool> HasUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await context.Categories.AnyAsync(a => a.Name == name, cancellationToken);
    }
}
