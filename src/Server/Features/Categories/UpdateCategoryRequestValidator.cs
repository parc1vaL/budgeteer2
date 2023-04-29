using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Categories;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    private readonly BudgetContext context;

    public UpdateCategoryRequestValidator(BudgetContext context)
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
