namespace Budgeteer.Blazor.Models;

public class EditBudgetModel
{
    public required DateOnly Date { get; init; }

    public required int CategoryId { get; init; }

    public required string Category { get; init; } = string.Empty;

    public required decimal Budget { get; set; }
}
