using System.Text.Json.Serialization;
using Budgeteer.Shared;

namespace Budgeteer.Server.Entities;

public class Transaction
{
    public int Id { get; set; }

    public required TransactionType TransactionType { get; set; }

    public required bool IsCleared { get; set; }

    public required int AccountId { get; set; }

    [JsonIgnore]
    public Account Account { get; set; } = null!;

    public required int? TransferAccountId { get; set; }

    [JsonIgnore]
    public Account? TransferAccount { get; set; }

    public required int? TransferTransactionId { get; set; }

    [JsonIgnore]
    public Transaction? TransferTransaction { get; set; }

    public required int? CategoryId { get; set; }

    [JsonIgnore]
    public Category? Category { get; set; }

    public required DateOnly Date { get; set; }

    public required string? Payee { get; set; }

    public required IncomeType IncomeType { get; set; }

    public required decimal Amount { get; set; }
}
