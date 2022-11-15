using System.Text.Json.Serialization;

namespace Budgeteer.Server.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; } =
        new List<Transaction>();

    [JsonIgnore]
    public ICollection<Budget> Budgets { get; } =
        new List<Budget>();
}
