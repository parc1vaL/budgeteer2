using System.Net.Mime;
using Budgeteer.Server.Entities;
using Budgeteer.Server.Logic.Accounts;
using Budgeteer.Shared.Accounts;
using Budgeteer.Shared.Transactions;

namespace Budgeteer.Server.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccounts(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Accounts";

        app.MapGet("/api/accounts", (AccountService service) => service.GetAccountsAsync())
            .WithName(Operations.Accounts.GetList)
            .WithTags(GroupName);

        app.MapGet("/api/accounts/{id:int}", (int id, AccountService service) => service.GetAccountAsync(id))
            .WithName(Operations.Accounts.GetDetails)
            .WithTags(GroupName)
            .Produces<AccountListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/accounts", (CreateAccountRequest request, AccountService service) => service.CreateAccountAsync(request))
            .WithName(Operations.Accounts.Create)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/accounts/{id:int}", (int id, UpdateAccountRequest request, AccountService service) => service.UpdateAccountAsync(id, request))
            .WithName(Operations.Accounts.Update)
            .WithTags(GroupName)
            .Produces<Account>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/accounts/{id:int}", (int id, AccountService service) => service.DeleteAccountAsync(id))
            .WithName(Operations.Accounts.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}