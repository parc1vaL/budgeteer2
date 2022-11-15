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

        app.MapGet("/api/categories", (CategoryService service) => service.GetCategoriesAsync())
            .WithName(Operations.Categories.GetList)
            .WithTags(GroupName);

        app.MapGet("/api/categories/{id:int}", (int id, CategoryService service) => service.GetCategoryAsync(id))
            .WithName(Operations.Categories.GetDetails)
            .WithTags(GroupName)
            .Produces<CategoryListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/categories", (CreateCategoryRequest request, CategoryService service) => service.CreateCategoryAsync(request))
            .WithName(Operations.Categories.Create)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/categories/{id:int}", (int id, UpdateCategoryRequest request, CategoryService service) => service.UpdateCategoryAsync(id, request))
            .WithName(Operations.Categories.Update)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/categories/{id:int}", (int id, CategoryService service) => service.DeleteCategoryAsync(id))
            .WithName(Operations.Categories.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}