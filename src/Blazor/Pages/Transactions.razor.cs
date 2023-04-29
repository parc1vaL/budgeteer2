using System.Net.Http.Json;
using Budgeteer.Blazor.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Action = Budgeteer.Blazor.Models.Action;

namespace Budgeteer.Blazor.Pages;

public partial class Transactions
{
    private Transaction[]? transactions;

    [Inject] private HttpClient HttpClient { get; set; } = null!;

    [Inject] private IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        this.transactions = await LoadTransactions();
    }

    private async Task OnRowClick(DataGridRowClickEventArgs<Transaction> item)
    {
        var parameters = new DialogParameters
        {
            [nameof(EditTransactionDialog.TransactionId)] = item.Item.Id,
        };

        var options = new DialogOptions { FullWidth = true, CloseOnEscapeKey = true, DisableBackdropClick = true,};
        var dialog = await this.DialogService.ShowAsync<EditTransactionDialog>("Edit transaction", parameters, options);
        var dialogResult = await dialog.Result;

        if (dialogResult.Canceled)
        {
            return;
        }

        this.transactions = null;
        StateHasChanged();

        switch (((EditTransactionDialogResult)dialogResult.Data).Action)
        {
            case Action.Delete:
                await this.HttpClient.DeleteAsync($"/transactions/{item.Item.Id}");
                break;
            case Action.Save:
                var transaction = ((EditTransactionDialogResult)dialogResult.Data).Model!;

                var request = new UpdateTransactionRequest
                {
                    TransactionType = transaction.TransactionType,
                    IsCleared = transaction.IsCleared,
                    AccountId = transaction.Account.Id,
                    Date = DateOnly.FromDateTime(transaction.Date ?? DateTime.Today),
                    Amount = transaction.Amount,
                    Payee = transaction.ShowPayee ? transaction.Payee : default,
                    TransferAccountId = transaction.ShowTransferAccount ? transaction.TransferAccount.Id : default(int?),
                    IncomeType = transaction.ShowIncomeType ? transaction.IncomeType : IncomeType.None,
                    CategoryId = transaction.ShowCategory ? transaction.Category.Id : default(int?),
                };
        
                await this.HttpClient.PutAsJsonAsync($"/transactions/{transaction.Id}", request);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown dialog result: {((EditTransactionDialogResult)dialogResult.Data).Action}");
        }
        
        
        this.transactions = await LoadTransactions();
    }

    private async Task CreateTransaction()
    {
        var parameters = new DialogParameters
        {
            [nameof(EditTransactionDialog.TransactionId)] = null,
        };

        var options = new DialogOptions { FullWidth = true, CloseOnEscapeKey = true, DisableBackdropClick = true,};
        var dialog = await this.DialogService.ShowAsync<EditTransactionDialog>("Create transaction", parameters, options);
        var dialogResult = await dialog.Result;

        if (dialogResult.Canceled)
        {
            return;
        }
        
        var transaction = ((EditTransactionDialogResult)dialogResult.Data).Model!;

        var request = new CreateTransactionRequest
        {
            TransactionType = transaction.TransactionType,
            IsCleared = transaction.IsCleared,
            AccountId = transaction.Account.Id,
            Date = DateOnly.FromDateTime(transaction.Date ?? DateTime.Today),
            Amount = transaction.Amount,
            Payee = transaction.ShowPayee ? transaction.Payee : default,
            TransferAccountId = transaction.ShowTransferAccount ? transaction.TransferAccount.Id : default(int?),
            IncomeType = transaction.ShowIncomeType ? transaction.IncomeType : IncomeType.None,
            CategoryId = transaction.ShowCategory ? transaction.Category.Id : default(int?),
        };

        this.transactions = null;
        StateHasChanged();
        
        await this.HttpClient.PostAsJsonAsync("/transactions", request);
        this.transactions = await LoadTransactions();
    }

    private async Task<Transaction[]> LoadTransactions()
    {
        var result = await this.HttpClient.GetFromJsonAsync<GetTransactionsResponse[]>("/transactions") 
                     ?? Array.Empty<GetTransactionsResponse>();
        return result.Select(transaction => new Transaction
        {
            Account = transaction.Account,
            Amount = transaction.Amount,
            Category = transaction.Category,
            Date = transaction.Date,
            Id = transaction.Id,
            Payee = transaction.Payee,
            AccountId = transaction.AccountId,
            CategoryId = transaction.CategoryId,
            IncomeType = transaction.IncomeType,
            IsCleared = transaction.IsCleared,
            TransactionType = transaction.TransactionType,
            TransferAccount = transaction.TransferAccount,
            TransferAccountId = transaction.TransferAccountId,
        }).ToArray();
    }
}
