using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Budgeteer.Blazor;

public partial class MainLayout
{
    private GetAccountResponse[] accounts = Array.Empty<GetAccountResponse>();

    private bool offBudgetExpanded = true;
    private bool onBudgetExpanded = true;

    [Inject] private HttpClient HttpClient { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        accounts = await HttpClient.GetFromJsonAsync<GetAccountResponse[]>("/api/accounts")
                   ?? Array.Empty<GetAccountResponse>();
    }
}
