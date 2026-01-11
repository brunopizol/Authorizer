using Authorizer.Application.Metrics;
using Authorizer.Domain.Entities;
using Authorizer.Domain.Events;
using Authorizer.FraudService;
using Authorizer.FraudService.Dto;
using Authorizer.Infrastructure.EventStore;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Application.Handlers
{
    public class AuthorizeTransactionHandler
    {
        private readonly IFraudAnalyzer _fraudAnalyzer;
        private readonly IEventStore _eventStore;
        private readonly IMetricsCollector _metrics;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<AuthorizeTransactionHandler> _logger;
        private static readonly TimeSpan SlaLimit = TimeSpan.FromMilliseconds(1500);

        public AuthorizeTransactionHandler(
            IEventStore eventStore,
            IFraudAnalyzer fraudAnalyzer,
            IPublishEndpoint publishEndpoint,
            ILogger<AuthorizeTransactionHandler> logger,
            IMetricsCollector metrics)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _fraudAnalyzer = fraudAnalyzer ?? throw new ArgumentNullException(nameof(fraudAnalyzer));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task HandleAsync(
            PurchasePayload payload,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var streamId = $"transaction-{payload.TransactionId}";

            try
            {
                await _publishEndpoint.Publish(payload, ct);

                _logger.LogInformation(
                    "Transaction {TransactionId} queued for authorization",
                    payload.TransactionId);


            }
            catch (Exception ex)
            {
                sw.Stop();
                throw new ApplicationException(
                    $"Error authorizing transaction {payload.TransactionId} after {sw.ElapsedMilliseconds} ms", ex);
            }
        }
    }

}
