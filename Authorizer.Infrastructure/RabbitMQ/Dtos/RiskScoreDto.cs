using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.RabbitMQ.Dtos
{
    public record RiskScoreDto
    {
        public double Score { get; init; }
        public string RiskLevel { get; init; }
        public int TransactionVelocity { get; init; }
        public bool AvsMatch { get; init; }
        public bool CvcMatch { get; init; }
        public string SpendingPattern { get; init; }
        public string IpCountry { get; init; }
        public bool CountryMatch { get; init; }
        public int FailedAttempts { get; init; }
        public bool IsBlacklisted { get; init; }
        public DateTime ScoreGeneratedAt { get; init; }
    }
}
