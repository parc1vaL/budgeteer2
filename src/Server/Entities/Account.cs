using System.Text.Json.Serialization;

namespace Budgeteer.Server.Entities;

public class Account
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required bool OnBudget { get; set; }

    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; } =
        new List<Transaction>();
}