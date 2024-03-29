namespace Budgeteer.Shared;

public class GetTransactionsResponse
{
    public required int Id { get; init; }

    public required TransactionType TransactionType { get; init; }

    public required bool IsCleared { get; init; }

    public required int AccountId { get; init; }

    public required string Account { get; init; } = string.Empty;

    public required int? TransferAccountId { get; set; }

    public required string? TransferAccount { get; set; }

    public required int? CategoryId { get; init; }

    public required string? Category { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Payee { get; init; }

    public required IncomeType IncomeType { get; init; }

    public required decimal Amount { get; init; }
}
