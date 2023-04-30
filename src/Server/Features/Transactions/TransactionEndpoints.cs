using System.Net.Mime;

namespace Budgeteer.Server.Features.Transactions;

public static class TransactionEndpoints
{
    public static void MapTransactions(this IEndpointRouteBuilder application)
    {
        const string GroupName = "Transactions";

        application
            .MapGet("/api/transactions",
                (TransactionService service, CancellationToken cancellationToken) =>
                    service.GetTransactionsAsync(cancellationToken))
            .WithName(Operations.Transactions.GetList)
            .WithTags(GroupName)
            .Produces<GetTransactionsResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

        application
            .MapGet("/api/transactions/{id:int}",
                (int id, TransactionService service, CancellationToken cancellationToken) =>
                    service.GetTransactionAsync(id, cancellationToken))
            .WithName(Operations.Transactions.GetDetails)
            .WithTags(GroupName)
            .Produces<GetTransactionsResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        application
            .MapPost("/api/transactions",
                (CreateTransactionRequest request, TransactionService service, CancellationToken cancellationToken) =>
                    service.CreateTransactionAsync(request, cancellationToken))
            .WithName(Operations.Transactions.Create)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

        application
            .MapPut("/api/transactions/{id:int}",
                (int id, UpdateTransactionRequest request, TransactionService service,
                        CancellationToken cancellationToken) =>
                    service.UpdateTransactionAsync(id, request, cancellationToken))
            .WithName(Operations.Transactions.Update)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound);

        application
            .MapDelete("/api/transactions/{id:int}",
                (int id, TransactionService service, CancellationToken cancellationToken) =>
                    service.DeleteTransactionAsync(id, cancellationToken))
            .WithName(Operations.Transactions.Delete)
            .WithTags(GroupName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
