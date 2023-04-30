using System.Net.Http.Json;
using Budgeteer.Blazor.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Action = Budgeteer.Blazor.Models.Action;

namespace Budgeteer.Blazor.Pages;

public partial class EditTransactionDialog : IDisposable
{
    private IValidator<EditTransactionModel> validator = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;

    private GetAccountResponse[] Accounts { get; set; } = Array.Empty<GetAccountResponse>();
    private GetCategoriesResponse[] Categories { get; set; } = Array.Empty<GetCategoriesResponse>();

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public int? TransactionId { get; set; }

    public EditTransactionModel? Transaction { get; set; }

    public void Dispose()
    {
        if (Transaction is not null)
        {
            Transaction.VisibilityUpdated -= Render;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Accounts = await HttpClient.GetFromJsonAsync<GetAccountResponse[]>("/api/accounts")
                   ?? Array.Empty<GetAccountResponse>();
        Categories = await HttpClient.GetFromJsonAsync<GetCategoriesResponse[]>("/api/categories")
                     ?? Array.Empty<GetCategoriesResponse>();

        validator = new EditTransactionModelValidator(Accounts, Categories);

        if (TransactionId.HasValue)
        {
            var response = await HttpClient.GetFromJsonAsync<GetTransactionsResponse>($"/api/transactions/{TransactionId}");

            if (response is not null)
            {
                var account = Accounts.First(account => account.Id == response.AccountId);
                var transferAccount = response.TransferAccountId.HasValue
                    ? Accounts.First(transfer => transfer.Id == response.TransferAccountId)
                    : null;
                var category = response.CategoryId.HasValue
                    ? Categories.First(category => category.Id == response.CategoryId)
                    : null;

                Transaction = new EditTransactionModel
                {
                    Id = response.Id,
                    TransactionType = response.TransactionType,
                    IncomeType = response.IncomeType,
                    Date = response.Date.ToDateTime(default),
                    Account = account,
                    TransferAccount = transferAccount,
                    Category = category,
                    Payee = response.Payee,
                    Amount = response.Amount,
                    IsCleared = response.IsCleared
                };

                Transaction.VisibilityUpdated += Render;
            }
        }
        else
        {
            Transaction = new EditTransactionModel
            {
                Id = null,
                TransactionType = TransactionType.External,
                IncomeType = IncomeType.None,
                Date = DateTime.Today,
                Account = Accounts.FirstOrDefault(),
                TransferAccount = null,
                Category = Categories.FirstOrDefault(),
                Payee = string.Empty,
                Amount = 0.0M,
                IsCleared = false
            };

            Transaction.VisibilityUpdated += Render;
        }
    }

    private void Render(object? sender, EventArgs? args)
    {
        StateHasChanged();
    }

    private void Delete()
    {
        MudDialog.Close(
            new EditTransactionDialogResult { Action = Action.Delete, Model = null });
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Submit()
    {
        MudDialog.Close(
            new EditTransactionDialogResult { Action = Action.Save, Model = Transaction });
    }
}
