namespace Budgeteer.Server.Features.Categories.Contracts.Response;

public class CategoryListItem
{
    public required int Id { get; init; }

    public required string Name { get; init; }
}