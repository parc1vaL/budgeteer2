namespace Budgeteer.Server.Logic.Budgets;

public class GetBudgetRequest
{
    public required int Year { get; init; }

    public required int Month { get; init; }
}