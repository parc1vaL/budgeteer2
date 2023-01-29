namespace Budgeteer.Server.Features.Budgets.Contracts.Request;

public class GetBudgetRequest
{
    public required int Year { get; init; }

    public required int Month { get; init; }
}