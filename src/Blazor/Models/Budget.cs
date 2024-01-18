// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Budgeteer.Blazor.Models;

public class Budget
{
    public required DateOnly Date { get; init; }

    public required GetBudgetResponse Item { get; init; }
}
