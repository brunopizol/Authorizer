using Authorizer.FraudService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.FraudService
{
    public class FraudAnalyzer : IFraudAnalyzer
    {
        private static readonly TimeSpan MaxAnalysisTime = TimeSpan.FromMilliseconds(800);

        public async Task<FraudAnalysisResult> AnalyzeAsync(
            PurchasePayloadDto payload,
            CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(MaxAnalysisTime);

            try
            {
                var checks = await Task.WhenAll(
                    CheckBlacklist(payload, cts.Token),
                    CheckVelocity(payload, cts.Token),
                    CheckRiskScore(payload, cts.Token),
                    CheckCountryMatch(payload, cts.Token),
                    CheckAvsAndCvc(payload, cts.Token),
                    CheckSpendingPattern(payload, cts.Token)
                );

                return AggregateResults(checks);
            }
            catch (OperationCanceledException)
            {
                return new FraudAnalysisResult
                {
                    IsApproved = false,
                    Reason = "Fraud check timeout",
                    RiskLevel = "HIGH"
                };
            }
        }

        private async Task<CheckResult> CheckBlacklist(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(50, ct); // Simula consulta DB

            if (p.RiskScore?.IsBlacklisted == true)
            {
                return new CheckResult
                {
                    Passed = false,
                    Reason = "Card in blacklist",
                    Weight = 1000
                };
            }

            return CheckResult.Success();
        }

        private async Task<CheckResult> CheckVelocity(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(100, ct);

            var velocity = p.RiskScore?.TransactionVelocity ?? 0;

            if (velocity > 10) // Mais de 10 transações em janela curta
            {
                return new CheckResult
                {
                    Passed = false,
                    Reason = "High transaction velocity",
                    Weight = 300
                };
            }

            return CheckResult.Success();
        }

        private async Task<CheckResult> CheckRiskScore(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(150, ct);

            var score = p.RiskScore?.Score ?? 0;

            return score switch
            {
                >= 800 => new CheckResult { Passed = false, Reason = "Critical risk score", Weight = 500 },
                >= 500 => new CheckResult { Passed = false, Reason = "High risk score", Weight = 300 },
                >= 300 => new CheckResult { Passed = true, Reason = "Medium risk", Weight = 100 },
                _ => CheckResult.Success()
            };
        }

        private async Task<CheckResult> CheckCountryMatch(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(50, ct);

            if (p.RiskScore?.CountryMatch == false)
            {
                return new CheckResult
                {
                    Passed = false,
                    Reason = "Country mismatch",
                    Weight = 200
                };
            }

            return CheckResult.Success();
        }

        private async Task<CheckResult> CheckAvsAndCvc(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(80, ct);

            var avsMatch = p.RiskScore?.AvsMatch ?? "U";
            var cvcMatch = p.RiskScore?.CvcMatch ?? "U";

            if (avsMatch == "N" || cvcMatch == "N")
            {
                return new CheckResult
                {
                    Passed = false,
                    Reason = "AVS/CVC mismatch",
                    Weight = 400
                };
            }

            return CheckResult.Success();
        }

        private async Task<CheckResult> CheckSpendingPattern(PurchasePayloadDto p, CancellationToken ct)
        {
            await Task.Delay(120, ct);

            var pattern = p.RiskScore?.SpendingPattern ?? "NORMAL";

            return pattern switch
            {
                "SUSPICIOUS" => new CheckResult { Passed = false, Reason = "Suspicious pattern", Weight = 600 },
                "UNUSUAL" => new CheckResult { Passed = true, Reason = "Unusual pattern", Weight = 150 },
                _ => CheckResult.Success()
            };
        }

        private FraudAnalysisResult AggregateResults(CheckResult[] checks)
        {
            var totalWeight = checks.Sum(c => c.Weight);
            var failedChecks = checks.Where(c => !c.Passed).ToList();

            if (failedChecks.Any(c => c.Weight >= 500))
            {
                return new FraudAnalysisResult
                {
                    IsApproved = false,
                    Reason = string.Join(", ", failedChecks.Select(c => c.Reason)),
                    RiskLevel = "CRITICAL",
                    TotalRiskScore = totalWeight
                };
            }

            if (totalWeight >= 400)
            {
                return new FraudAnalysisResult
                {
                    IsApproved = false,
                    Reason = "Cumulative risk too high",
                    RiskLevel = "HIGH",
                    TotalRiskScore = totalWeight
                };
            }

            return new FraudAnalysisResult
            {
                IsApproved = true,
                RiskLevel = totalWeight > 100 ? "MEDIUM" : "LOW",
                TotalRiskScore = totalWeight
            };
        }
    }

}
