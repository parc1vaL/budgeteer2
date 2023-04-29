using System.Net.Http.Json;
using Budgeteer.Blazor.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Action = Budgeteer.Blazor.Models.Action;

namespace Budgeteer.Blazor.Pages;

public partial class EditTransactionDialog : IDisposable
{
    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    private GetAccountResponse[] Accounts { get; set; } = Array.Empty<GetAccountResponse>();
    private GetCategoriesResponse[] Categories { get; set; } = Array.Empty<GetCategoriesResponse>();

    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public int? TransactionId { get; set; }

    private IValidator<EditTransactionModel> validator = null!;

    public EditTransactionModel? Transaction { get; set; }

    protected override async Task OnInitializedAsync()
    {
        this.Accounts = await this.HttpClient.GetFromJsonAsync<GetAccountResponse[]>("accounts")
                        ?? Array.Empty<GetAccountResponse>();
        this.Categories = await this.HttpClient.GetFromJsonAsync<GetCategoriesResponse[]>("categories") 
                          ?? Array.Empty<GetCategoriesResponse>();

        this.validator = new EditTransactionModelValidator(this.Accounts, this.Categories);

        if (this.TransactionId.HasValue)
        {
            var response = await this.HttpClient.GetFromJsonAsync<GetTransactionsResponse>($"transactions/{this.TransactionId}");

            if (response is not null)
            {
                var account = this.Accounts.First(account => account.Id == response.AccountId);
                var transferAccount = response.TransferAccountId.HasValue
                    ? this.Accounts.First(transfer => transfer.Id == response.TransferAccountId)
                    : null;
                var category = response.CategoryId.HasValue
                    ? this.Categories.First(category => category.Id == response.CategoryId)
                    : null;
            
                this.Transaction = new EditTransactionModel
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
                    IsCleared = response.IsCleared,
                };

                this.Transaction.VisibilityUpdated += Render;
            }
        }
        else
        {
            this.Transaction = new EditTransactionModel
            {
                Id = null,
                TransactionType = TransactionType.External,
                IncomeType = IncomeType.None,
                Date = DateTime.Today,
                Account = this.Accounts.FirstOrDefault(),
                TransferAccount = null,
                Category = this.Categories.FirstOrDefault(),
                Payee = string.Empty,
                Amount = 0.0M,
                IsCleared = false,
            };

            this.Transaction.VisibilityUpdated += Render;
        }
        
    }

    public void Dispose()
    {
        if (this.Transaction is not null)
        {
            this.Transaction.VisibilityUpdated -= Render;
        }
    }

    private void Render(object? sender, EventArgs? args) => StateHasChanged();

    private void Delete() => MudDialog.Close(
        new EditTransactionDialogResult { Action = Action.Delete, Model = null, });

    private void Cancel() => MudDialog.Cancel();
    
    private void Submit() => MudDialog.Close(
        new EditTransactionDialogResult { Action = Action.Save, Model = this.Transaction });
}
