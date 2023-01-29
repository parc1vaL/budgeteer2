using System.Text.Json.Serialization;
using Budgeteer.Server.Features.Categories;

namespace Budgeteer.Server.Features.Budgets;

public class Budget
{
    public required int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public required DateOnly Month { get; set; }

    public required decimal Amount { get; set; }
}
