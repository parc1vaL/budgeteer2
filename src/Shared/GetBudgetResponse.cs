namespace Budgeteer.Shared;

public class GetBudgetResponse
{
    public required decimal LeftoverBudget { get; init; }

    public required decimal Income { get; init; }

    public decimal ToBeBudgeted =>
        this.LeftoverBudget
        + this.Income
        - this.Budgets.Sum(b => b.CurrentBudget);

    public required GetBudgetResponseItem[] Budgets { get; init; }
}
