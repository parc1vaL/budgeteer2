using System.Net.Mime;
using Budgeteer.Server.Entities;
using Budgeteer.Server.Logic.Accounts;
using Budgeteer.Shared.Accounts;

namespace Budgeteer.Server.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccounts(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Accounts";

        app.MapGet("/api/accounts", (AccountService service, CancellationToken cancellationToken) => service.GetAccountsAsync(cancellationToken))
            .WithName(Operations.Accounts.GetList)
            .WithTags(GroupName)
            .Produces<AccountListItem[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/accounts/{id:int}", (int id, AccountService service, CancellationToken cancellationToken) => service.GetAccountAsync(id, cancellationToken))
            .WithName(Operations.Accounts.GetDetails)
            .WithTags(GroupName)
            .Produces<AccountListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/accounts", (CreateAccountRequest request, AccountService service, CancellationToken cancellationToken) => service.CreateAccountAsync(request, cancellationToken))
            .WithName(Operations.Accounts.Create)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/accounts/{id:int}", (int id, UpdateAccountRequest request, AccountService service, CancellationToken cancellationToken) => service.UpdateAccountAsync(id, request, cancellationToken))
            .WithName(Operations.Accounts.Update)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/accounts/{id:int}", (int id, AccountService service, CancellationToken cancellationToken) => service.DeleteAccountAsync(id, cancellationToken))
            .WithName(Operations.Accounts.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}