using Budgeteer.Server.Features.Budgets;
using Budgeteer.Server.Features.Transactions;

namespace Budgeteer.Server.Features.Categories;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Transaction> Transactions { get; } =
        new List<Transaction>();

    public ICollection<Budget> Budgets { get; } =
        new List<Budget>();
}
