namespace Budgeteer.Blazor.Models;

public class EditTransactionModel
{
    private GetAccountResponse? account;
    private IncomeType incomeType;
    private TransactionType transactionType;
    private GetAccountResponse? transferAccount;

    public required int? Id { get; init; }

    public required TransactionType TransactionType
    {
        get => transactionType;
        set
        {
            transactionType = value;
            VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required bool IsCleared { get; set; }

    public required GetAccountResponse? Account
    {
        get => account;
        set
        {
            account = value;
            VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required GetAccountResponse? TransferAccount
    {
        get => transferAccount;
        set
        {
            transferAccount = value;
            VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required GetCategoriesResponse? Category { get; set; }

    public required DateTime? Date { get; set; }

    public required string? Payee { get; set; }

    public required IncomeType IncomeType
    {
        get => incomeType;
        set
        {
            incomeType = value;
            VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required decimal Amount { get; set; }

    public bool ShowIncomeType =>
        (TransactionType == TransactionType.External && (Account?.OnBudget ?? false))
        || (TransactionType == TransactionType.Internal && (Account?.OnBudget ?? false) != TransferAccount?.OnBudget);

    public bool ShowTransferAccount => TransactionType == TransactionType.Internal;

    public bool ShowCategory =>
        (TransactionType == TransactionType.External && IncomeType == IncomeType.None)
        || (TransactionType == TransactionType.Internal
            && (Account?.OnBudget ?? false) != TransferAccount?.OnBudget
            && IncomeType == IncomeType.None);

    public bool ShowPayee => TransactionType == TransactionType.External;

    public event EventHandler? VisibilityUpdated;
}
