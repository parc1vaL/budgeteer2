using System.Text.Json.Serialization;

namespace Budgeteer.Server.Entities;

public class Budget
{
    public required int CategoryId { get; set; }

    [JsonIgnore]
    public Category Category { get; set; } = null!;

    public required DateOnly Month { get; set; }

    public required decimal Amount { get; set; }
}
