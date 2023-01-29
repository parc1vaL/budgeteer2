using FluentValidation;

namespace Budgeteer.Server.Features.Budgets.Contracts.Request;

public class GetBudgetValidator : AbstractValidator<GetBudgetRequest>
{
    public GetBudgetValidator()
    {
        RuleFor(v => v.Month)
            .GreaterThanOrEqualTo(1).WithMessage("Month value must be between 1 and 12.")
            .LessThanOrEqualTo(12).WithMessage("Month value must be between 1 and 12.");

        RuleFor(v => v.Year)
            .GreaterThanOrEqualTo(1900).WithMessage("Year value must be between 1900 and 2100.")
            .LessThanOrEqualTo(2100).WithMessage("Year value must be between 1900 and 2100.");
    }
}