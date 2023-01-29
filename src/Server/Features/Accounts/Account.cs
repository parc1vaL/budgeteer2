using Budgeteer.Server.Features.Transactions;

namespace Budgeteer.Server.Features.Accounts;

public class Account
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required bool OnBudget { get; set; }

    public ICollection<Transaction> Transactions { get; } =
        new List<Transaction>();
}
