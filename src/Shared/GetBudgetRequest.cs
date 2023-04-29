namespace Budgeteer.Shared;

public class GetBudgetRequest
{
    public required int Year { get; init; }

    public required int Month { get; init; }
}