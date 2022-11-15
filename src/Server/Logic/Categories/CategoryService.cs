using Budgeteer.Server.Entities;
using Budgeteer.Shared.Categories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server.Logic.Categories;

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

    public async Task<CategoryListItem[]> GetCategoriesAsync()
    {
        return await this.context.Categories
            .AsNoTracking()
            .Select(a => new CategoryListItem
            {
                Id = a.Id,
                Name = a.Name,
            })
            .ToArrayAsync();
    }

    public async Task<IResult> GetCategoryAsync(int id)
    {
        var result = await this.context.Categories
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new CategoryDetails { Id = a.Id, Name = a.Name, })
            .FirstOrDefaultAsync();

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound();
    }

    public async Task<IResult> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var validationResult = await this.createValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var category = new Category
        {
            Name = request.Name,
        };

        this.context.Categories.Add(category);

        await this.context.SaveChangesAsync();

        return Results.Created(
            this.linkGenerator.GetPathByName(Operations.Categories.GetDetails, new() { ["id"] = category.Id, })
                ?? throw new InvalidOperationException("Resource path could not be generated."),
            category);
    }

    public async Task<IResult> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await this.context.Categories.FindAsync(id);

        if (category is null)
        {
            return Results.NotFound();
        }

        var validationResult = await this.updateValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        category.Name = request.Name;

        await this.context.SaveChangesAsync();

        return Results.Ok(category);
    }

    public async Task<IResult> DeleteCategoryAsync(int id)
    {
        var category = await this.context.Categories.FindAsync(id);

        if (category is null)
        {
            return Results.NotFound();
        }

        this.context.Categories.Remove(category);
        await this.context.SaveChangesAsync();

        return Results.Ok();
    }
}