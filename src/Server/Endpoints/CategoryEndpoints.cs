using System.Net.Mime;
using Budgeteer.Server.Entities;
using Budgeteer.Server.Logic.Categories;
using Budgeteer.Shared.Categories;

namespace Budgeteer.Server.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategories(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Categories";

        app.MapGet("/api/categories", (CategoryService service, CancellationToken cancellationToken) => service.GetCategoriesAsync(cancellationToken))
            .WithName(Operations.Categories.GetList)
            .WithTags(GroupName);

        app.MapGet("/api/categories/{id:int}", (int id, CategoryService service, CancellationToken cancellationToken) => service.GetCategoryAsync(id, cancellationToken))
            .WithName(Operations.Categories.GetDetails)
            .WithTags(GroupName)
            .Produces<CategoryListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/categories", (CreateCategoryRequest request, CategoryService service, CancellationToken cancellationToken) => service.CreateCategoryAsync(request, cancellationToken))
            .WithName(Operations.Categories.Create)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/categories/{id:int}", (int id, UpdateCategoryRequest request, CategoryService service, CancellationToken cancellationToken) => service.UpdateCategoryAsync(id, request, cancellationToken))
            .WithName(Operations.Categories.Update)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/categories/{id:int}", (int id, CategoryService service, CancellationToken cancellationToken) => service.DeleteCategoryAsync(id, cancellationToken))
            .WithName(Operations.Categories.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}