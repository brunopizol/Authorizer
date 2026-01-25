using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class TransactionDeniedEvent : DomainEvent
    {
        public override object GetPayload() =>new { Reason, TransactionId, DeniedAt };
        public string TransactionId { get; init; }
        public string Reason { get; init; }
        public DateTime DeniedAt { get; init; }
    }
}
