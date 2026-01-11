using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Authorizer.Domain.Events;
using Authorizer.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.EventStore
{
    public class DynamoDbEventStore : IEventStore
    {
        private readonly IAmazonDynamoDB _client;
        private const string TableName = "event-store";
        private static readonly Dictionary<string, Type> EventTypeCache = InitializeEventTypeCache();

        public DynamoDbEventStore(IAmazonDynamoDB client)
        {
            _client = client;
        }

        public async Task AppendAsync(string streamId, DomainEvent @event, CancellationToken ct)
        {
            // Obter a próxima versão para este stream
            var nextVersion = await GetStreamVersionAsync(streamId, ct) + 1;

            var item = new Dictionary<string, AttributeValue>
            {
                ["stream_id"] = new AttributeValue { S = streamId },
                ["version"] = new AttributeValue { N = nextVersion.ToString() },
                ["event_id"] = new AttributeValue { S = @event.EventId.ToString() },
                ["event_type"] = new AttributeValue { S = @event.EventType },
                ["event_data"] = new AttributeValue { S = JsonSerializer.Serialize(@event) },
                ["occurred_at"] = new AttributeValue { S = @event.OccurredAt.ToString("O") },
                ["aggregate_id"] = new AttributeValue { S = @event.AggregateId }
            };

            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(#sid) OR attribute_not_exists(#vid)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#sid"] = "stream_id",
                    ["#vid"] = "version"
                }
            };

            try
            {
                await _client.PutItemAsync(request, ct);
            }
            catch (ConditionalCheckFailedException)
            {
                throw new ConcurrencyException($"Event version {nextVersion} already exists for stream {streamId}");
            }
        }

        public async Task<IEnumerable<DomainEvent>> GetEventsAsync(string streamId, CancellationToken ct)
        {
            var request = new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "stream_id = :streamId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":streamId"] = new AttributeValue { S = streamId }
                },
                ScanIndexForward = true // Ordem crescente por version
            };

            var response = await _client.QueryAsync(request, ct);

            return response.Items
                .Select(item => DeserializeEvent(item["event_data"].S, item["event_type"].S))
                .ToList();
        }

        public async Task<IEnumerable<DomainEvent>> GetEventsByTypeAsync(
            string eventType,
            DateTime from,
            DateTime to,
            CancellationToken ct)
        {
            var request = new QueryRequest
            {
                TableName = TableName,
                IndexName = "event-type-index",
                KeyConditionExpression = "event_type = :eventType AND occurred_at BETWEEN :from AND :to",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":eventType"] = new AttributeValue { S = eventType },
                    [":from"] = new AttributeValue { S = from.ToString("O") },
                    [":to"] = new AttributeValue { S = to.ToString("O") }
                }
            };

            var response = await _client.QueryAsync(request, ct);

            return response.Items
                .Select(item => DeserializeEvent(item["event_data"].S, item["event_type"].S))
                .ToList();
        }

        public async Task<long> GetStreamVersionAsync(string streamId, CancellationToken ct)
        {
            var request = new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "stream_id = :streamId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":streamId"] = new AttributeValue { S = streamId }
                },
                ScanIndexForward = false, // Ordem decrescente
                Limit = 1,
                ProjectionExpression = "version"
            };

            var response = await _client.QueryAsync(request, ct);

            if (response.Items.Count == 0)
                return 0;

            return long.Parse(response.Items[0]["version"].N);
        }

        private DomainEvent DeserializeEvent(string eventJson, string eventType)
        {
            if (!EventTypeCache.TryGetValue(eventType, out var type))
            {
                throw new InvalidOperationException($"Unknown event type: {eventType}");
            }

            return (DomainEvent)JsonSerializer.Deserialize(eventJson, type)!;
        }

        private static Dictionary<string, Type> InitializeEventTypeCache()
        {
            var cache = new Dictionary<string, Type>();
            var assembly = typeof(DomainEvent).Assembly;
            var eventTypes = assembly.GetTypes()
                .Where(t => typeof(DomainEvent).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in eventTypes)
            {
                var instance = Activator.CreateInstance(type) as DomainEvent;
                if (instance != null)
                {
                    cache[instance.EventType] = type;
                }
            }

            return cache;
        }
    }
}
