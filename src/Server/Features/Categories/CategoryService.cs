using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Categories;

public class CategoryService(
    BudgetContext context,
    LinkGenerator linkGenerator,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<UpdateCategoryRequest> updateValidator)
{
    public async Task<IResult> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await context.Categories
                .Select(a => new GetCategoriesResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                })
                .ToArrayAsync(cancellationToken));
    }

    public async Task<IResult> GetCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var result = await context.Categories
            .Select(a => new GetCategoriesResponse { Id = a.Id, Name = a.Name, })
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    public async Task<IResult> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var category = new Category
        {
            Name = request.Name,
        };

        context.Categories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            linkGenerator.GetPathByName(Operations.Categories.GetDetails, new() { ["id"] = category.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    public async Task<IResult> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return TypedResults.NotFound();
        }

        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        category.Name = request.Name;

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    public async Task<IResult> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken: cancellationToken);

        if (category is null)
        {
            return TypedResults.NotFound();
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
