using Budgeteer.Blazor.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Budgeteer.Blazor.Pages;

public partial class EditBudgetDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public EditBudgetModel Model { get; set; } = null!;

    private MudNumericField<decimal>? AmountField { get; set; }

    private void Cancel() => this.MudDialog.Cancel();

    private void Submit() => this.MudDialog.Close(this.Model);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await (this.AmountField?.FocusAsync() ?? ValueTask.CompletedTask);
    }
}
