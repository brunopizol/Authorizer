using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.RabbitMQ.Dtos
{
    public record AuthorizeTransactionCommand
    {
        public string TransactionId { get; init; }
        public string CorrelationId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; }
        public string CardId { get; init; }
        public string Pan { get; init; }
        public string PanHash { get; init; }
        public string Account { get; init; }
        public string Merchant { get; init; }
        public string MerchantCountry { get; init; }
        public string EntryMode { get; init; }
        public string Mcc { get; init; }
        public string AcquirerCode { get; init; }
        public string Nsu { get; init; }
        public string AuthorizationCode { get; init; }
        public string ReferenceNumber { get; init; }
        public DateTime Timestamp { get; init; }
        public string CardExpiration { get; init; }
        public string CardHolder { get; init; }
        public int? Installments { get; init; }
        public RiskScoreDto RiskScore { get; init; }
        public Dictionary<string, object> AdditionalData { get; init; }
    }
}
