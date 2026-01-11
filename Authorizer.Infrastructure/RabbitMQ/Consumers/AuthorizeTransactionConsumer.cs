using Authorizer.Application.Metrics;
using Authorizer.Domain.Entities;
using Authorizer.Domain.Events;
using Authorizer.FraudService;
using Authorizer.FraudService.Dto;
using Authorizer.Infrastructure.EventStore;
using Authorizer.Infrastructure.RabbitMQ.Dtos;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.RabbitMQ.Consumers
{
    public class AuthorizeTransactionConsumer : IConsumer<PurchasePayload>
    {
        private readonly IFraudAnalyzer _fraudAnalyzer;
        private readonly IEventStore _eventStore;
        private readonly ILogger<AuthorizeTransactionConsumer> _logger;
        private static readonly TimeSpan SlaLimit = TimeSpan.FromMilliseconds(1500);
        private IMetricsCollector _metrics;

        public AuthorizeTransactionConsumer(
            IEventStore eventStore,
            IFraudAnalyzer fraudAnalyzer,
            IMetricsCollector metrics,
            ILogger<AuthorizeTransactionConsumer> logger)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _fraudAnalyzer = fraudAnalyzer ?? throw new ArgumentNullException(nameof(fraudAnalyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<PurchasePayload> context)
        {
            var sw = Stopwatch.StartNew();
            var command = context.Message;
            var streamId = $"transaction-{command.TransactionId}";

            _logger.LogInformation(
                "Processing transaction {TransactionId} - Amount: {Amount} {Currency}",
                command.TransactionId, command.Amount, command.Currency);

            try
            {
                // 1. Event: Transaction Received
                await PublishEventAsync(streamId, new TransactionReceivedEvent(command)
                {
                    AggregateId = streamId
                }, context.CancellationToken);

                // 2. Event: Fraud Check Started
                await PublishEventAsync(streamId, new FraudCheckStartedEvent
                {
                    AggregateId = streamId,
                    TransactionId = command.TransactionId,
                    StartedAt = DateTime.UtcNow
                }, context.CancellationToken);

                // 3. Execute Fraud Analysis
                var fraudResult = await _fraudAnalyzer.AnalyzeAsync(
                                ToDto(command),
                                context.CancellationToken);

                // 4. Event: Fraud Check Completed
                await PublishEventAsync(streamId, new FraudCheckCompletedEvent
                {
                    AggregateId = streamId,
                    TransactionId = command.TransactionId,
                    Result = fraudResult,
                    Duration = sw.Elapsed
                }, context.CancellationToken);

                // 5. Decision
                if (fraudResult.IsApproved)
                {
                    var authCode = GenerateAuthCode();

                    await PublishEventAsync(streamId, new TransactionAuthorizedEvent
                    {
                        AggregateId = streamId,
                        TransactionId = command.TransactionId,
                        AuthorizationCode = authCode,
                        AuthorizedAt = DateTime.UtcNow
                    }, context.CancellationToken);

                    // Publica evento de sucesso no barramento
                    await context.Publish(new TransactionAuthorizedEvent
                    {
                        TransactionId = command.TransactionId,
                        AuthorizationCode = authCode,
                        AuthorizedAt = DateTime.UtcNow,
                    });

                    _logger.LogInformation(
                        "Transaction {TransactionId} authorized with code {AuthCode}",
                        command.TransactionId, authCode);
                }
                else
                {
                    await PublishEventAsync(streamId, new TransactionDeniedEvent
                    {
                        AggregateId = streamId,
                        TransactionId = command.TransactionId,
                        Reason = fraudResult.Reason,
                        DeniedAt = DateTime.UtcNow
                    }, context.CancellationToken);

                    await context.Publish(new TransactionDeniedEvent
                    {
                        TransactionId = command.TransactionId,
                        Reason = fraudResult.Reason,
                        DeniedAt = DateTime.UtcNow,
                    });

                    _logger.LogWarning(
                        "Transaction {TransactionId} denied. Reason: {Reason}",
                        command.TransactionId, fraudResult.Reason);
                }

                sw.Stop();

                if (sw.Elapsed > SlaLimit)
                {
                    await PublishEventAsync(streamId, new SlaViolationEvent
                    {
                        AggregateId = streamId,
                        TransactionId = command.TransactionId,
                        ActualDuration = sw.Elapsed,
                        SlaLimit = SlaLimit
                    }, context.CancellationToken);

                    _metrics.RecordSlaViolation(command.TransactionId, sw.Elapsed);

                    _logger.LogWarning(
                        "SLA violation for transaction {TransactionId}. Duration: {Duration}ms",
                        command.TransactionId, sw.Elapsed.TotalMilliseconds);
                }

                _metrics.RecordAuthorization(sw.Elapsed, fraudResult.IsApproved);
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(ex,
                    "Error processing transaction {TransactionId}",
                    command.TransactionId);

                await PublishEventAsync(streamId, new TransactionDeniedEvent
                {
                    AggregateId = streamId,
                    TransactionId = command.TransactionId,
                    Reason = $"System error: {ex.Message}",
                    DeniedAt = DateTime.UtcNow
                }, context.CancellationToken);

                // Publica evento de erro
                await context.Publish(new TransactionDeniedEvent
                {
                    TransactionId = command.TransactionId,
                    Reason = "System unavailable",
                    DeniedAt = DateTime.UtcNow,
                });

                if (sw.Elapsed > SlaLimit)
                {
                    _metrics.RecordSlaViolation(command.TransactionId, sw.Elapsed);
                }

                // Re-lança a exceção para que o MassTransit trate (retry, error queue, etc)
                throw;
            }
        }

        private async Task PublishEventAsync(string streamId, DomainEvent @event, CancellationToken ct)
        {
            await _eventStore.AppendAsync(streamId, @event, ct);
        }

        private static string GenerateAuthCode()
        {
            return $"AUTH{Random.Shared.Next(100000, 999999)}";
        }

        private static PurchasePayloadDto ToDto(PurchasePayload payload)
        {
            return new PurchasePayloadDto
            {
                TransactionId = payload.TransactionId,
                CorrelationId = payload.CorrelationId,
                Amount = payload.Amount,
                Currency = payload.Currency,
                CardId = payload.CardId,
                Pan = payload.Pan,
                PanHash = payload.PanHash,
                Account = payload.Account,
                Merchant = payload.Merchant,
                MerchantCountry = payload.MerchantCountry,
                EntryMode = payload.EntryMode,
                Mcc = payload.Mcc,
                AcquirerCode = payload.AcquirerCode,
                Nsu = payload.Nsu,
                AuthorizationCode = payload.AuthorizationCode,
                ReferenceNumber = payload.ReferenceNumber,
                Timestamp = payload.Timestamp,
                CardExpiration = payload.CardExpiration,
                CardHolder = payload.CardHolder,
                Installments = payload.Installments,
                RiskScore = payload.RiskScore == null ? null : new Authorizer.FraudService.Dto.RiskScoreData
                {
                    Score = payload.RiskScore.Score,
                    RiskLevel = payload.RiskScore.RiskLevel,
                    TransactionVelocity = payload.RiskScore.TransactionVelocity,
                    AvsMatch = payload.RiskScore.AvsMatch,
                    CvcMatch = payload.RiskScore.CvcMatch,
                    SpendingPattern = payload.RiskScore.SpendingPattern,
                    IpCountry = payload.RiskScore.IpCountry,
                    CountryMatch = payload.RiskScore.CountryMatch,
                    FailedAttempts = payload.RiskScore.FailedAttempts,
                    IsBlacklisted = payload.RiskScore.IsBlacklisted,
                    ScoreGeneratedAt = payload.RiskScore.ScoreGeneratedAt
                },
                AdditionalData = payload.AdditionalData
            };
        }
    }
}
