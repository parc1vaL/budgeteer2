namespace Budgeteer.Blazor.Models;

public class EditTransactionDialogResult
{
    public required Action Action { get; set; }

    public required EditTransactionModel? Model { get; set; }
}

public enum Action
{
    Save,
    Delete,
}
