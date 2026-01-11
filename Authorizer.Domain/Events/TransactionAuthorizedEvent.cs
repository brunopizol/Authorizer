using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public class TransactionAuthorizedEvent : DomainEvent
    {
        public string TransactionId { get; init; }
        public string AuthorizationCode { get; init; }
        public DateTime AuthorizedAt { get; init; }
    }
}
