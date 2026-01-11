using Authorizer.FraudService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.FraudService
{
    public interface IFraudAnalyzer
    {
        Task<FraudAnalysisResult> AnalyzeAsync(PurchasePayloadDto payload, CancellationToken ct);
    }
}
