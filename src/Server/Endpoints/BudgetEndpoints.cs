using System.Net.Mime;
using Budgeteer.Server.Entities;
using Budgeteer.Shared.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace Budgeteer.Server.Logic.Budgets;

public static class BudgetEndpoints
{
    public static void MapBudgets(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Budgets";

        app.MapGet(
            "/api/budgets/{year:int}/{month:int}", 
            (int year, int month, BudgetService service, CancellationToken cancellationToken) 
                => service.GetBudget(new GetBudgetRequest { Year = year, Month = month, }, cancellationToken))
            .WithName(Operations.Budgets.Get)
            .WithTags(GroupName)
            .Produces<BudgetMonth>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPost(
            "/api/budgets/{year:int}/{month:int}/{categoryId:int}",
            (int year, int month, int categoryId, [FromBody]decimal amount, BudgetService service, CancellationToken cancellationToken)
                => service.CreateOrUpdateBudget(new CreateOrUpdateBudgetRequest { Year = year, Month = month, CategoryId = categoryId, Amount = amount, }, cancellationToken))
            .WithName(Operations.Budgets.CreateOrUpdate)
            .WithTags(GroupName)
            .Produces<Budget>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);
    }
}