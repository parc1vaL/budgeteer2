namespace Budgeteer.Shared.Transactions;

public class UpdateTransactionRequest
{
    public required TransactionType TransactionType { get; init; }

    public required int AccountId { get; init; }

    public required int? TransactionAccountId { get; init; }

    // public required int? CategoryId { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Payee { get; init; }

    public required IncomeType IncomeType { get; init; }

    public required decimal Amount { get; init; }

    public required bool IsCleared { get; init; }
}