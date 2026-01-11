using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class FraudCheckStartedEvent : DomainEvent
    {

        public string TransactionId { get; init; }
        public DateTime StartedAt { get; init; }
    }
}
