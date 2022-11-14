namespace Budgeteer.Shared.Accounts;

public record AccountDetails
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required bool OnBudget { get; init; }
}