// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MudBlazor;

namespace Budgeteer.Blazor.Models;

public class Transaction
{
    public required int Id { get; init; }

    public required TransactionType TransactionType { get; init; }

    public required bool IsCleared { get; init; }

    public required int AccountId { get; init; }

    public required string Account { get; init; } = string.Empty;

    public required int? TransferAccountId { get; set; }

    public required string? TransferAccount { get; set; }

    public required int? CategoryId { get; init; }

    public required string? Category { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Payee { get; init; }

    public required IncomeType IncomeType { get; init; }

    public required decimal Amount { get; init; }

    public string CategoryDisplay => this.Category ?? this.IncomeType switch
    {
        IncomeType.CurrentMonth => "Income (current month)",
        IncomeType.NextMonth => "Income (next month)",
        IncomeType.None => string.Empty,
        _ => throw new InvalidOperationException($"Unknown income type: {this.IncomeType}"),
    };

    public string PayeeDisplay => this.Payee ?? $"Transfer ({this.TransferAccount})";
}
