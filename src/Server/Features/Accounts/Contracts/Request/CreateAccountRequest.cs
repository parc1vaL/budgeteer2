namespace Budgeteer.Server.Features.Accounts.Contracts.Request;

public class CreateAccountRequest 
{
    public required string Name { get; init; }

    public required bool OnBudget { get; init; }

    public required decimal Balance { get; init; }
}