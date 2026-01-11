using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Authorizer.FraudService.Dto
{
    public class PurchasePayloadDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CardId { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string PanHash { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Merchant { get; set; } = string.Empty;
        public string MerchantCountry { get; set; } = string.Empty;
        public string EntryMode { get; set; } = string.Empty;
        public string Mcc { get; set; } = string.Empty;
        public string AcquirerCode { get; set; } = string.Empty;
        public string Nsu { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string CardExpiration { get; set; } = string.Empty;
        public string CardHolder { get; set; } = string.Empty;
        public int Installments { get; set; }

        // ISO 8583 Risk Score Fields
        public RiskScoreData? RiskScore { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Dados de risco baseados em ISO 8583 (campo 48 - Additional Data)
    /// </summary>
    public class RiskScoreData
    {
        /// <summary>
        /// Score de risco geral (0-999)
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Nível de risco: LOW, MEDIUM, HIGH, CRITICAL
        /// </summary>
        public string RiskLevel { get; set; } = "LOW";

        /// <summary>
        /// Indicador de velocidade de transações (ISO 8583 campo 48)
        /// </summary>
        public int TransactionVelocity { get; set; }

        /// <summary>
        /// Indicador de correspondência de AVS (Address Verification System)
        /// </summary>
        public string AvsMatch { get; set; } = "Y"; // Y, N, U

        /// <summary>
        /// Indicador de correspondência de CVC (Card Verification Code)
        /// </summary>
        public string CvcMatch { get; set; } = "Y"; // Y, N, U

        /// <summary>
        /// Indicador de padrão de gasto
        /// </summary>
        public string SpendingPattern { get; set; } = "NORMAL"; // NORMAL, UNUSUAL, SUSPICIOUS

        /// <summary>
        /// Indicador de país de origem do IP
        /// </summary>
        public string IpCountry { get; set; } = "BR";

        /// <summary>
        /// Indicador de correspondência de país do cartão com IP
        /// </summary>
        public bool CountryMatch { get; set; } = true;

        /// <summary>
        /// Número de tentativas falhadas anteriores (ISO 8583)
        /// </summary>
        public int FailedAttempts { get; set; } = 0;

        /// <summary>
        /// Indicador de cartão em lista negra/positiva
        /// </summary>
        public bool IsBlacklisted { get; set; } = false;

        /// <summary>
        /// Data de geração do score
        /// </summary>
        public DateTime ScoreGeneratedAt { get; set; }
    }
}
