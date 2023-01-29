using Budgeteer.Server.Features.Budgets.Contracts.Request;
using Budgeteer.Server.Features.Budgets.Contracts.Response;
using Budgeteer.Server.Features.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Budgets;

public class BudgetService
{
    private readonly BudgetContext context;
    private readonly LinkGenerator linkGenerator;
    private readonly IValidator<GetBudgetRequest> getValidator;
    private readonly IValidator<CreateOrUpdateBudgetRequest> createOrUpdateValidator;

    public BudgetService(
        BudgetContext context,
        LinkGenerator linkGenerator,
        IValidator<GetBudgetRequest> getValidator,
        IValidator<CreateOrUpdateBudgetRequest> createOrUpdateValidator)
    {
        this.context = context;
        this.linkGenerator = linkGenerator;
        this.getValidator = getValidator;
        this.createOrUpdateValidator = createOrUpdateValidator;
    }

    public async Task<IResult> GetBudget(GetBudgetRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await this.getValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Gets the budget items
            var items = await GetItemsQuery(request)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var startDate = new DateOnly(request.Year, request.Month, 1);
            var prevDate = startDate.AddMonths(-1);
            var endDate = startDate.AddMonths(1);

            // Gets the total income in on-budget accounts for the request month
            var income = await this.context.Transactions
                .Where(t =>
                    (t.Date < endDate
                     && t.Date >= startDate
                     && t.IncomeType == IncomeType.CurrentMonth)
                    || (t.Date < startDate
                        && t.Date >= prevDate
                        && t.IncomeType == IncomeType.NextMonth))
                .AsNoTracking()
                .SumAsync(t => t.Amount, cancellationToken)
                .ConfigureAwait(false);

            // Gets the total income in on-budget accounts prior to the request month
            var incomePrevious = await this.context.Transactions
                .Where(t =>
                    (t.Date < startDate
                     && t.IncomeType == IncomeType.CurrentMonth)
                    || (t.Date < prevDate
                        && t.IncomeType == IncomeType.NextMonth))
                .AsNoTracking()
                .SumAsync(t => t.Amount, cancellationToken)
                .ConfigureAwait(false);

            // Gets the total budget amount over all categories prior to the request month
            var budgetPrevious = await this.context.Budgets
                .Where(b => b.Month < startDate)
                .SumAsync(b => b.Amount, cancellationToken)
                .ConfigureAwait(false);

        return Results.Ok(
            new BudgetMonth 
            {
                Income = income, 
                LeftoverBudget = incomePrevious - budgetPrevious, 
                Budgets = items,
            });
    }

    public async Task<IResult> CreateOrUpdateBudget(CreateOrUpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await this.createOrUpdateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var budget = await this.context.Budgets
            .FirstOrDefaultAsync(
                b => b.CategoryId == request.CategoryId && b.Month == new DateOnly(request.Year, request.Month, 1),
                cancellationToken)
            .ConfigureAwait(false);

        if (budget is null)
        {
            budget = new Budget
            {
                CategoryId = request.CategoryId,
                Month = new DateOnly(request.Year, request.Month, 1),
                Amount = request.Amount,
            };

            this.context.Budgets.Add(budget);
        }
        else
        {
            budget.Amount = request.Amount;
        }

        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(budget);
    }

    private IQueryable<BudgetMonthItem> GetItemsQuery(GetBudgetRequest request)
    {
        var budgetDate = new DateOnly(request.Year, request.Month, 1);
        var nextMonth = budgetDate.AddMonths(1);

        // For each category, gets the allocated budget for the request month
        var currentBudgetQuery =
            from c in this.context.Categories
            join b in this.context.Budgets
                on new { Id = c.Id, Month = budgetDate }
                equals new { Id = b.CategoryId, Month = b.Month }
                into budgets
            from b in budgets.DefaultIfEmpty()
            select new { CategoryId = c.Id, CategoryName = c.Name, CurrentBudget = (decimal?)b.Amount ?? 0.0m, };

        // For each category, gets the sum of all allocated budgets prior to the request month
        var previousBudgetQuery =
            from c in this.context.Categories
            join b in this.context.Budgets
                on new { Id = c.Id, Month = true }
                equals new { Id = b.CategoryId, Month = b.Month < budgetDate }
                into budgets
            from b in budgets.DefaultIfEmpty()
            group new { b.Amount } by new { Id = c.Id }
            into g
            select new { CategoryId = g.Key.Id, PreviousBudget = g.Sum(m => m.Amount), };

        // For each category, gets the sum of all transactions within the request month in on-budget accounts
        var currentOutflowQuery =
            from c in this.context.Categories
            join t in this.context.Transactions
                on new { Id = c.Id, After = true, Before = true } 
                equals new { Id = t.CategoryId!.Value, After = t.Date >= budgetDate, Before = t.Date < nextMonth, }
                into transactions
            from t in transactions.DefaultIfEmpty()
            join a in this.context.Accounts 
                on new { Id = t.AccountId, OnBudget = true, }
                equals new { Id = a.Id, OnBudget = a.OnBudget, }
                into accounts
            from a in accounts.DefaultIfEmpty()
            group new { t.Amount, } by new { Id = c.Id, }
            into g
            select new { CategoryId = g.Key.Id, CurrentTotal = g.Sum(m => m.Amount), };

        // For each category, gets the sum of all transactions prior to the request month in on-budget accounts
        var previousOutflowQuery =
            from c in this.context.Categories
            join t in this.context.Transactions
                on new { Id = c.Id, Before = true }
                equals new { Id = t.CategoryId!.Value, Before = t.Date < budgetDate }
                into transactions
            from t in transactions.DefaultIfEmpty()
            join a in this.context.Accounts 
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
            select new BudgetMonthItem
            {
                CategoryId = currentBudget.CategoryId,
                Category = currentBudget.CategoryName,
                PreviousBalance = previousBudget.PreviousBudget + previousOutflow.PreviousTotal,
                CurrentBudget = currentBudget.CurrentBudget,
                CurrentOutflow = currentOutflow.CurrentTotal,
            };
    }
}