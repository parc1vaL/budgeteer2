namespace Budgeteer.Shared.Budgets;

public class BudgetMonth
{
    public required decimal LeftoverBudget { get; init; }

    public required decimal Income { get; init; }

    public decimal ToBeBudgeted =>
        this.LeftoverBudget
        + this.Income
        - this.Budgets.Sum(b => b.CurrentBudget);

    public required BudgetMonthItem[] Budgets { get; init; }
}