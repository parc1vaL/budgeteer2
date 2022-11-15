namespace Budgeteer.Shared.Budgets;

public class BudgetMonthItem
{
    public required int CategoryId { get; init; }

    public required string Category { get; init; }

    public required decimal PreviousBalance { get; init; }

    public required decimal CurrentBudget { get; init; }

    public required decimal CurrentOutflow { get; init; }

    public decimal RemainingBudget =>
        this.PreviousBalance
        + this.CurrentBudget
        + this.CurrentOutflow;
}