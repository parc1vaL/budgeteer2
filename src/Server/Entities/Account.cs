namespace Budgeteer.Server.Entities;

public class Account
{
    public int Id { get; set; }

    public required string Name { get; set; } = string.Empty;

    public required bool OnBudget { get; set; }

    public ICollection<Transaction> Transactions { get; } =
        new List<Transaction>();
}