namespace Budgeteer.Shared.Accounts;

public class AccountDetails
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required bool OnBudget { get; init; }

    public required decimal Balance { get; init; }
}