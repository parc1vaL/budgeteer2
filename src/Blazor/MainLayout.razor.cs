using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Budgeteer.Blazor;

public partial class MainLayout
{
    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    private GetAccountResponse[] accounts = Array.Empty<GetAccountResponse>();
    
    private bool offBudgetExpanded = true;
    private bool onBudgetExpanded = true;

    protected override async Task OnInitializedAsync()
    {
        this.accounts = await this.HttpClient.GetFromJsonAsync<GetAccountResponse[]>("/accounts") 
                        ?? Array.Empty<GetAccountResponse>();
    }
}
