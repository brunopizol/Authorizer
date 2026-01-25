using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class SlaViolationEvent : DomainEvent
    {
        public override object GetPayload() =>new { TransactionId, ActualDuration, SlaLimit };
        public string TransactionId { get; init; }
        public TimeSpan ActualDuration { get; init; }
        public TimeSpan SlaLimit { get; init; }
    }
}
