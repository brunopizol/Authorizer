using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.FraudService
{
    public record FraudAnalysisResult
    {
        public bool IsApproved { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string RiskLevel { get; init; } = "LOW";
        public int TotalRiskScore { get; init; }
    }
}
