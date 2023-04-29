using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Budgeteer.Server.Features.Budgets;

public static class BudgetEndpoints
{
    public static void MapBudgets(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Budgets";

        app.MapGet(
            "/budgets/{year:int}/{month:int}", 
            (int year, int month, BudgetService service, CancellationToken cancellationToken) 
                => service.GetBudget(new GetBudgetRequest { Year = year, Month = month, }, cancellationToken))
            .WithName(Operations.Budgets.Get)
            .WithTags(GroupName)
            .Produces<GetBudgetResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut(
            "/budgets/{year:int}/{month:int}/{categoryId:int}",
            (int year, int month, int categoryId, [FromBody]decimal amount, BudgetService service, CancellationToken cancellationToken)
                => service.CreateOrUpdateBudget(new CreateOrUpdateBudgetRequest { Year = year, Month = month, CategoryId = categoryId, Amount = amount, }, cancellationToken))
            .WithName(Operations.Budgets.CreateOrUpdate)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);
    }
}
