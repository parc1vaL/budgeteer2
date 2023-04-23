using Budgeteer.Server.Features.Categories.Contracts.Request;
using Budgeteer.Server.Features.Categories.Contracts.Response;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Features.Categories;

public class CategoryService
{
    private readonly BudgetContext context;
    private readonly LinkGenerator linkGenerator;
    private readonly IValidator<CreateCategoryRequest> createValidator;
    private readonly IValidator<UpdateCategoryRequest> updateValidator;

    public CategoryService(
        BudgetContext context,
        LinkGenerator linkGenerator,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        this.context = context;
        this.linkGenerator = linkGenerator;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    public async Task<IResult> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await this.context.Categories
                .Select(a => new CategoryListItem
                {
                    Id = a.Id,
                    Name = a.Name,
                })
                .ToArrayAsync(cancellationToken));
    }

    public async Task<IResult> GetCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var result = await this.context.Categories
            .Select(a => new CategoryDetails { Id = a.Id, Name = a.Name, })
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    public async Task<IResult> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await this.createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var category = new Category
        {
            Name = request.Name,
        };

        this.context.Categories.Add(category);

        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            this.linkGenerator.GetPathByName(Operations.Categories.GetDetails, new() { ["id"] = category.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."));
    }

    public async Task<IResult> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await this.context.Categories.FindAsync(new object[] { id, }, cancellationToken);

        if (category is null)
        {
            return TypedResults.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        category.Name = request.Name;

        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    public async Task<IResult> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await this.context.Categories.FindAsync(new object[] { id, }, cancellationToken: cancellationToken);

        if (category is null)
        {
            return TypedResults.NotFound();
        }

        this.context.Categories.Remove(category);
        await this.context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
