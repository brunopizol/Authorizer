using Authorizer.Domain.Events;
using Authorizer.Infrastructure.RabbitMQ.Consumers;
using MassTransit;

namespace Authorizer.Api.IoC
{
    public static class MassTransitConfiguration
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                // Registra o consumer
                x.AddConsumer<AuthorizeTransactionConsumer>(cfg =>
                {
                    // Configuração de retry
                    cfg.UseMessageRetry(r =>
                    {
                        r.Exponential(5, // 5 tentativas
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(2));

                        // Ignora exceções de validação
                        r.Ignore<ArgumentException>();
                        r.Ignore<ArgumentNullException>();
                    });

                    // Circuit breaker para proteger dependências
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = 15;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval = TimeSpan.FromMinutes(5);
                    });

                    // Rate limiter para controlar throughput
                    cfg.UseRateLimit(1000, TimeSpan.FromSeconds(1));
                });

                // Configuração do RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqConfig = configuration.GetSection("RabbitMQ");

                    cfg.Host(rabbitMqConfig["Host"] ?? "localhost", rabbitMqConfig["VirtualHost"] ?? "/", h =>
                    {
                        h.Username(rabbitMqConfig["Username"] ?? "guest");
                        h.Password(rabbitMqConfig["Password"] ?? "guest");
                    });

                    // Configuração de prefetch - quantas mensagens buscar por vez
                    cfg.PrefetchCount = 16;

                    // Configuração da fila de autorização
                    cfg.ReceiveEndpoint("authorize-transaction-queue", e =>
                    {
                        // Configurações de concorrência
                        e.ConcurrentMessageLimit = 10;

                        // TTL da mensagem (time to live)
                        e.SetQueueArgument("x-message-ttl", 300000); // 5 minutos

                        // Dead letter exchange para mensagens que falharam
                        e.SetQueueArgument("x-dead-letter-exchange", "authorize-transaction-dlx");

                        // Configura o consumer
                        e.ConfigureConsumer<AuthorizeTransactionConsumer>(context);
                    });

                    // Configuração de filas para eventos publicados
                    cfg.Message<TransactionAuthorizedEvent>(m => m.SetEntityName("transaction-authorized"));
                    cfg.Message<TransactionDeniedEvent>(m => m.SetEntityName("transaction-denied"));

                    // Configurações globais
                    cfg.UseMessageRetry(r => r.Intervals(100, 500, 1000));
                    cfg.UseInMemoryOutbox();
                });
            });

            return services;
        }
    }
}
