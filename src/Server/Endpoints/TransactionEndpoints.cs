using System.Net.Mime;
using Budgeteer.Server.Entities;
using Budgeteer.Server.Logic.Transactions;
using Budgeteer.Shared.Transactions;

namespace Budgeteer.Server.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactions(this IEndpointRouteBuilder app) 
    {
        const string GroupName = "Transactions";

        app.MapGet("/api/transactions", (TransactionService service, CancellationToken cancellationToken) => service.GetTransactionsAsync(cancellationToken))
            .WithName(Operations.Transactions.GetList)
            .WithTags(GroupName);

        app.MapGet("/api/transactions/{id:int}", (int id, TransactionService service, CancellationToken cancellationToken) => service.GetTransactionAsync(id, cancellationToken))
            .WithName(Operations.Transactions.GetDetails)
            .WithTags(GroupName)
            .Produces<TransactionListItem>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/transactions", (CreateTransactionRequest request, TransactionService service, CancellationToken cancellationToken) => service.CreateTransactionAsync(request, cancellationToken))
            .WithName(Operations.Transactions.Create)
            .WithTags(GroupName)
            .Produces<Transaction>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/transactions/{id:int}", (int id, UpdateTransactionRequest request, TransactionService service, CancellationToken cancellationToken) => service.UpdateTransactionAsync(id, request, cancellationToken))
            .WithName(Operations.Transactions.Update)
            .WithTags(GroupName)
            .Produces<Transaction>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/transactions/{id:int}", (int id, TransactionService service, CancellationToken cancellationToken) => service.DeleteTransactionAsync(id, cancellationToken))
            .WithName(Operations.Transactions.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}