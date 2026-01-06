using Authorizer.Api.Endpoints;
using Authorizer.Api.IoC;
using Authorizer.Application.Handlers;
using Authorizer.Application.Metrics;
using Authorizer.FraudService;
using Authorizer.Infrastructure.Setup;

var builder = WebApplication.CreateBuilder(args);

// DynamoDB Event Store
builder.Services.AddDynamoDbEventStore(builder.Configuration);

// Fraud Service
builder.Services.AddSingleton<IFraudAnalyzer, FraudAnalyzer>();

// Handler
builder.Services.AddScoped<AuthorizeTransactionHandler>();

// Metrics
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);

var app = builder.Build();

// Initialize DynamoDB Table
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DynamoDbTableInitializer>();
    await initializer.InitializeAsync();
}

app.MapAuthorizationEndpoints();

app.Run();