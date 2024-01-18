using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Budgets;

public class BudgetService(
    BudgetContext context,
    IValidator<GetBudgetRequest> getValidator,
    IValidator<CreateOrUpdateBudgetRequest> createOrUpdateValidator)
{
    public async Task<IResult> GetBudget(GetBudgetRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await getValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        // Gets the budget items
        var items = await GetItemsQuery(request).ToArrayAsync(cancellationToken);

        var startDate = new DateOnly(request.Year, request.Month, 1);
        var prevDate = startDate.AddMonths(-1);
        var endDate = startDate.AddMonths(1);

        // Gets the total income in on-budget accounts for the request month
        var income = await context.Transactions
            .Where(t =>
                (t.Date < endDate
                 && t.Date >= startDate
                 && t.IncomeType == IncomeType.CurrentMonth)
                || (t.Date < startDate
                    && t.Date >= prevDate
                    && t.IncomeType == IncomeType.NextMonth))
            .SumAsync(t => t.Amount, cancellationToken);

        // Gets the total income in on-budget accounts prior to the request month
        var incomePrevious = await context.Transactions
            .Where(t =>
                (t.Date < startDate
                 && t.IncomeType == IncomeType.CurrentMonth)
                || (t.Date < prevDate
                    && t.IncomeType == IncomeType.NextMonth))
            .SumAsync(t => t.Amount, cancellationToken);

        // Gets the total budget amount over all categories prior to the request month
        var budgetPrevious = await context.Budgets
            .Where(b => b.Month < startDate)
            .SumAsync(b => b.Amount, cancellationToken);

        return TypedResults.Ok(
            new GetBudgetResponse
            {
                Income = income,
                LeftoverBudget = incomePrevious - budgetPrevious,
                Budgets = items,
            });
    }

    public async Task<IResult> CreateOrUpdateBudget(CreateOrUpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createOrUpdateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var budget = await context.Budgets
            .FirstOrDefaultAsync(
                b => b.CategoryId == request.CategoryId && b.Month == new DateOnly(request.Year, request.Month, 1),
                cancellationToken);

        if (budget is null)
        {
            budget = new Budget
            {
                CategoryId = request.CategoryId,
                Month = new DateOnly(request.Year, request.Month, 1),
                Amount = request.Amount,
            };

            context.Budgets.Add(budget);
        }
        else
        {
            budget.Amount = request.Amount;
        }

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    private IQueryable<GetBudgetResponseItem> GetItemsQuery(GetBudgetRequest request)
    {
        var budgetDate = new DateOnly(request.Year, request.Month, 1);
        var nextMonth = budgetDate.AddMonths(1);

        // For each category, gets the allocated budget for the request month
        var currentBudgetQuery =
            from c in context.Categories
            join b in context.Budgets
                on new { Id = c.Id, Month = budgetDate }
                equals new { Id = b.CategoryId, Month = b.Month }
                into budgets
            from b in budgets.DefaultIfEmpty()
            select new { CategoryId = c.Id, CategoryName = c.Name, CurrentBudget = (decimal?)b.Amount ?? 0.0m, };

        // For each category, gets the sum of all allocated budgets prior to the request month
        var previousBudgetQuery =
            from c in context.Categories
            join b in context.Budgets
                on new { Id = c.Id, Month = true }
                equals new { Id = b.CategoryId, Month = b.Month < budgetDate }
                into budgets
            from b in budgets.DefaultIfEmpty()
            group new { b.Amount } by new { Id = c.Id }
            into g
            select new { CategoryId = g.Key.Id, PreviousBudget = g.Sum(m => m.Amount), };

        // For each category, gets the sum of all transactions within the request month in on-budget accounts
        var currentOutflowQuery =
            from c in context.Categories
            join t in context.Transactions
                on new { Id = c.Id, After = true, Before = true }
                equals new { Id = t.CategoryId!.Value, After = t.Date >= budgetDate, Before = t.Date < nextMonth, }
                into transactions
            from t in transactions.DefaultIfEmpty()
            join a in context.Accounts
                on new { Id = t.AccountId, OnBudget = true, }
                equals new { Id = a.Id, OnBudget = a.OnBudget, }
                into accounts
            from a in accounts.DefaultIfEmpty()
            group new { t.Amount, } by new { Id = c.Id, }
            into g
            select new { CategoryId = g.Key.Id, CurrentTotal = g.Sum(m => m.Amount), };

        // For each category, gets the sum of all transactions prior to the request month in on-budget accounts
        var previousOutflowQuery =
            from c in context.Categories
            join t in context.Transactions
                on new { Id = c.Id, Before = true }
                equals new { Id = t.CategoryId!.Value, Before = t.Date < budgetDate }
                into transactions
            from t in transactions.DefaultIfEmpty()
            join a in context.Accounts
                on new { Id = t.AccountId, OnBudget = true, }
                equals new { Id = a.Id, OnBudget = a.OnBudget, }
                into accounts
            from a in accounts.DefaultIfEmpty()
            group new { t.Amount } by new { Id = c.Id }
            into g
            select new { CategoryId = g.Key.Id, PreviousTotal = g.Sum(m => m.Amount), };

        return from currentBudget in currentBudgetQuery
            join previousBudget in previousBudgetQuery
                on currentBudget.CategoryId equals previousBudget.CategoryId
            join currentOutflow in currentOutflowQuery
                on currentBudget.CategoryId equals currentOutflow.CategoryId
            join previousOutflow in previousOutflowQuery
                on currentBudget.CategoryId equals previousOutflow.CategoryId
            select new GetBudgetResponseItem
            {
                CategoryId = currentBudget.CategoryId,
                Category = currentBudget.CategoryName,
                PreviousBalance = previousBudget.PreviousBudget + previousOutflow.PreviousTotal,
                CurrentBudget = currentBudget.CurrentBudget,
                CurrentOutflow = currentOutflow.CurrentTotal,
            };
    }
}
