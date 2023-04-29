using System.Net.Mime;

namespace Budgeteer.Server.Features.Accounts;

public static class AccountEndpoints
{
    public static void MapAccounts(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Accounts";

        app.MapGet("/accounts", (AccountService service, CancellationToken cancellationToken) => service.GetAccountsAsync(cancellationToken))
            .WithName(Operations.Accounts.GetList)
            .WithTags(GroupName)
            .Produces<GetAccountResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

        app.MapGet("/accounts/{id:int}", (int id, AccountService service, CancellationToken cancellationToken) => service.GetAccountAsync(id, cancellationToken))
            .WithName(Operations.Accounts.GetDetails)
            .WithTags(GroupName)
            .Produces<GetAccountResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/accounts", (CreateAccountRequest request, AccountService service, CancellationToken cancellationToken) => service.CreateAccountAsync(request, cancellationToken))
            .WithName(Operations.Accounts.Create)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/accounts/{id:int}", (int id, UpdateAccountRequest request, AccountService service, CancellationToken cancellationToken) => service.UpdateAccountAsync(id, request, cancellationToken))
            .WithName(Operations.Accounts.Update)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/accounts/{id:int}", (int id, AccountService service, CancellationToken cancellationToken) => service.DeleteAccountAsync(id, cancellationToken))
            .WithName(Operations.Accounts.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
