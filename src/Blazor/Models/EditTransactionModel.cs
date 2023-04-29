namespace Budgeteer.Blazor.Models;

public class EditTransactionModel
{
    private TransactionType transactionType;
    private GetAccountResponse? account;
    private GetAccountResponse? transferAccount;
    private IncomeType incomeType;

    public required int? Id { get; init; }

    public required TransactionType TransactionType
    {
        get => transactionType;
        set
        {
            transactionType = value;
            this.VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required bool IsCleared { get; set; }

    public required GetAccountResponse? Account
    {
        get => account;
        set
        {
            account = value;
            this.VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required GetAccountResponse? TransferAccount
    {
        get => transferAccount;
        set
        {
            transferAccount = value;
            this.VisibilityUpdated?.Invoke(this, EventArgs.Empty);
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
            this.VisibilityUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public required decimal Amount { get; set; }

    public bool ShowIncomeType =>
        (this.TransactionType == TransactionType.External && this.Account.OnBudget) 
        || (this.TransactionType == TransactionType.Internal && this.Account.OnBudget != this.TransferAccount?.OnBudget);

    public bool ShowTransferAccount => this.TransactionType == TransactionType.Internal;

    public bool ShowCategory =>
        (this.TransactionType == TransactionType.External && this.IncomeType == IncomeType.None)
        || (this.TransactionType == TransactionType.Internal 
            && this.Account.OnBudget != this.TransferAccount?.OnBudget
            && this.IncomeType == IncomeType.None);

    public bool ShowPayee => this.TransactionType == TransactionType.External;

    public event EventHandler? VisibilityUpdated;
}
