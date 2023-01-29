using System.Text.Json.Serialization;
using Budgeteer.Server.Features.Budgets;
using Budgeteer.Server.Features.Transactions;

namespace Budgeteer.Server.Features.Categories;

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
