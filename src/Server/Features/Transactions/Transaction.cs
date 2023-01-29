using Budgeteer.Server.Features.Accounts;
using Budgeteer.Server.Features.Categories;

namespace Budgeteer.Server.Features.Transactions;

public class Transaction
{
    public int Id { get; set; }

    public required TransactionType TransactionType { get; set; }

    public required bool IsCleared { get; set; }

    public required int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public required int? TransferAccountId { get; set; }

    public Account? TransferAccount { get; set; }

    public required int? TransferTransactionId { get; set; }

    public Transaction? TransferTransaction { get; set; }

    public required int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public required DateOnly Date { get; set; }

    public required string? Payee { get; set; }

    public required IncomeType IncomeType { get; set; }

    public required decimal Amount { get; set; }
}
