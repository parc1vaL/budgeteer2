using System.Net.Http.Json;
using Budgeteer.Blazor.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Budgeteer.Blazor.Pages;

public partial class Budgets
{
    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public int? Month { get; set; }
    [Parameter] public int? Year { get; set; }

    private DateOnly Date { get; set; }

    private Budget[]? Months { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        this.Date = new DateOnly(this.Year ?? DateTime.Today.Year, this.Month ?? DateTime.Today.Month, 1);

        await LoadBudgets();
    }

    private async Task EditBudget(Budget month, GetBudgetResponseItem budget)
    {
        var parameter = new EditBudgetModel
        {
            Date = month.Date,
            CategoryId = budget.CategoryId,
            Category = budget.Category,
            Budget = budget.CurrentBudget,
        };
        
        var parameters = new DialogParameters
        {
            [nameof(EditBudgetDialog.Model)] = parameter,
        };

        var options = new DialogOptions { FullWidth = true, CloseOnEscapeKey = true, DisableBackdropClick = true,};
        var dialog = await this.DialogService.ShowAsync<EditBudgetDialog>("Edit Budget", parameters, options);
        var dialogResult = await dialog.Result;

        if (dialogResult.Canceled)
        {
            return;
        }

        var model = (EditBudgetModel)dialogResult.Data;

        this.Months = null;
        StateHasChanged();

        await this.HttpClient.PutAsJsonAsync(
            $"/budgets/{month.Date.Year}/{month.Date.Month}/{budget.CategoryId}",
            model.Budget);
        await LoadBudgets();
    }

    private async Task LoadBudgets()
    {
        var dates = new[] { this.Date.AddMonths(-1), this.Date, this.Date.AddMonths(1), };

        this.Months = await Task.WhenAll(dates.Select(async date =>
        {
            var response = await this.HttpClient.GetFromJsonAsync<GetBudgetResponse>($"budgets/{date.Year}/{date.Month}");

            return new Budget { Date = date, Item = response, };
        }));
    }
}
