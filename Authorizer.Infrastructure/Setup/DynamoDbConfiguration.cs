using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Authorizer.Infrastructure.EventStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authorizer.Infrastructure.Setup
{
    public static class DynamoDbConfiguration
    {
        public static IServiceCollection AddDynamoDbEventStore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var config = configuration.GetSection("DynamoDB");
            var serviceUrl = config["ServiceUrl"] ?? "http://localhost:8000";
            var region = config["Region"] ?? "us-east-1";

            services.AddSingleton<IAmazonDynamoDB>(sp =>
            {
                // Para DynamoDB Local, usa credenciais fictícias
                if (serviceUrl.Contains("localhost") || serviceUrl.Contains("127.0.0.1"))
                {
                    var clientConfig = new AmazonDynamoDBConfig
                    {
                        ServiceURL = serviceUrl  // ✅ CORRETO: ServiceUrl (não ServiceURL)
                    };

                    return new AmazonDynamoDBClient(
                        new BasicAWSCredentials("local", "local"),
                        clientConfig
                    );
                }

                // Para AWS real, usa credenciais padrão com RegionEndpoint
                var awsConfig = new AmazonDynamoDBConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
                };

                return new AmazonDynamoDBClient(awsConfig);
            });

            services.AddScoped<IEventStore, DynamoDbEventStore>();
            services.AddSingleton<DynamoDbTableInitializer>();

            return services;
        }
    }
}
