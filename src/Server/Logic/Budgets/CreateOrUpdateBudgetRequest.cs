using System.Text.Json.Serialization;

namespace Budgeteer.Server.Logic.Budgets;

public class CreateOrUpdateBudgetRequest
{
    public int Year { get; init; }

    public int Month { get; init; }

    public int CategoryId { get; init; }

    public required decimal Amount { get; init; }
}