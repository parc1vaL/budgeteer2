using Microsoft.AspNetCore.Components;

namespace Budgeteer.Blazor.Pages;

public partial class Index
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    protected override void OnInitialized()
    {
        this.NavigationManager.NavigateTo("/budget");
    }
}
