namespace Budgeteer.Server.Features.Transactions.Contracts.Request;

public class CreateTransactionRequest
{
    public required TransactionType TransactionType { get; init; }

    public required int AccountId { get; init; }

    public required int? TransferAccountId { get; init; }

    public required int? CategoryId { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Payee { get; init; }

    public required IncomeType IncomeType { get; init; }

    public required decimal Amount { get; init; }

    public required bool IsCleared { get; init; }
}