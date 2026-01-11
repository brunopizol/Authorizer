using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.Setup
{
    public class DynamoDbTableInitializer
    {
        private readonly IAmazonDynamoDB _client;

        public DynamoDbTableInitializer(IAmazonDynamoDB client)
        {
            _client = client;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            var tables = await _client.ListTablesAsync(ct);

            if (tables.TableNames.Contains("event-store"))
            {
                Console.WriteLine("✅ Table 'event-store' already exists");
                return;
            }

            var request = new CreateTableRequest
            {
                TableName = "event-store",
                KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = "stream_id", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "version", KeyType = KeyType.RANGE }
            },
                AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition { AttributeName = "stream_id", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "version", AttributeType = ScalarAttributeType.N },
                new AttributeDefinition { AttributeName = "event_type", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "occurred_at", AttributeType = ScalarAttributeType.S }
            },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "event-type-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = "event_type", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "occurred_at", KeyType = KeyType.RANGE }
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 5
                    }
                }
            },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            await _client.CreateTableAsync(request, ct);

            var tableActive = false;
            var maxRetries = 5;
            var delay = 2000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await Task.Delay(delay, ct);
                    var description = await _client.DescribeTableAsync("event-store", ct);
                    tableActive = description.Table.TableStatus == TableStatus.ACTIVE;
                    if (tableActive) break;
                }
                catch (HttpRequestException) when (i < maxRetries - 1)
                {
                    await Task.Delay(delay);
                }
            }

            Console.WriteLine("✅ Table 'event-store' created successfully");
        }
    }
}
