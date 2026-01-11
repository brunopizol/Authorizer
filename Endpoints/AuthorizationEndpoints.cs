using Authorizer.Application.Handlers;
using Authorizer.Application.Metrics;
using Authorizer.Domain.Entities;
using Authorizer.Infrastructure.EventStore;

namespace Authorizer.Api.Endpoints
{
    public static class AuthorizationEndpoints
    {
        public static void MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/authorization")
                .WithTags("Authorization")
                .WithOpenApi();

            group.MapPost("/", AuthorizeTransaction)
                .WithName("AuthorizeTransaction")
                .WithDescription("Autoriza uma transação de cartão com SLA de 1.5s");

            group.MapGet("/metrics", GetMetrics)
                .WithName("GetMetrics")
                .WithDescription("Retorna métricas de performance e SLA");

            group.MapGet("/events/{transactionId}", GetTransactionEvents)
                .WithName("GetTransactionEvents")
                .WithDescription("Retorna todos os eventos de uma transação");
        }

        private static async Task<IResult> AuthorizeTransaction(
            PurchasePayload payload,
            AuthorizeTransactionHandler handler,
            CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(1500));

            try
            {
                await handler.HandleAsync(payload, cts.Token);

                return Results.Ok();
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(408); // Request Timeout
            }
        }

        private static IResult GetMetrics(IMetricsCollector metrics)
        {
            var snapshot = metrics.GetSnapshot();
            return Results.Ok(snapshot);
        }

        private static async Task<IResult> GetTransactionEvents(
            string transactionId,
            IEventStore eventStore,
            CancellationToken ct)
        {
            var streamId = $"transaction-{transactionId}";
            var events = await eventStore.GetEventsAsync(streamId, ct);

            return Results.Ok(events);
        }
    }
}
