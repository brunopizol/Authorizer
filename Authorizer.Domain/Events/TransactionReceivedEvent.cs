using Authorizer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class TransactionReceivedEvent : DomainEvent
    {
        public PurchasePayload Payload { get; init; }

        public override object GetPayload() => Payload;

        public TransactionReceivedEvent() { }

        [JsonConstructor]
        public TransactionReceivedEvent(PurchasePayload payload)
        {
            Payload = payload;
        }
    }
}
