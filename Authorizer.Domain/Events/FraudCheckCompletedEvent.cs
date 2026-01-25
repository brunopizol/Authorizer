using Authorizer.FraudService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class FraudCheckCompletedEvent : DomainEvent
    {
        public override object GetPayload() => Result;

        public string TransactionId { get; init; }
        public FraudAnalysisResult Result { get; init; }
        public TimeSpan Duration { get; init; }
    }
}
