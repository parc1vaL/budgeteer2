using System.Net.Mime;
using Budgeteer.Server.Features.Categories.Contracts.Request;
using Budgeteer.Server.Features.Categories.Contracts.Response;

namespace Budgeteer.Server.Features.Categories;

public static class CategoryEndpoints
{
    public static void MapCategories(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Categories";

        app.MapGet("/categories", (CategoryService service, CancellationToken cancellationToken) => service.GetCategoriesAsync(cancellationToken))
            .WithName(Operations.Categories.GetList)
            .WithTags(GroupName);

        app.MapGet("/categories/{id:int}", (int id, CategoryService service, CancellationToken cancellationToken) => service.GetCategoryAsync(id, cancellationToken))
            .WithName(Operations.Categories.GetDetails)
            .WithTags(GroupName)
            .Produces<CategoryListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/categories", (CreateCategoryRequest request, CategoryService service, CancellationToken cancellationToken) => service.CreateCategoryAsync(request, cancellationToken))
            .WithName(Operations.Categories.Create)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/categories/{id:int}", (int id, UpdateCategoryRequest request, CategoryService service, CancellationToken cancellationToken) => service.UpdateCategoryAsync(id, request, cancellationToken))
            .WithName(Operations.Categories.Update)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/categories/{id:int}", (int id, CategoryService service, CancellationToken cancellationToken) => service.DeleteCategoryAsync(id, cancellationToken))
            .WithName(Operations.Categories.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
