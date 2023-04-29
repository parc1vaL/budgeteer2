using System.Net.Http.Json;
using Budgeteer.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace Budgeteer.Blazor.Pages;

public partial class Budgets
{
    [Inject] private HttpClient HttpClient { get; set; } = null!;

    [Parameter] public int? Month { get; set; }
    [Parameter] public int? Year { get; set; }

    private DateOnly Date { get; set; }

    private Budget[]? Months { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        this.Date = new DateOnly(this.Year ?? DateTime.Today.Year, this.Month ?? DateTime.Today.Month, 1);

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
