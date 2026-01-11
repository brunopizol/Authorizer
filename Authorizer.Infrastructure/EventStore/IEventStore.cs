using Authorizer.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Infrastructure.EventStore
{
    public interface IEventStore
    {
        Task AppendAsync(string streamId, DomainEvent @event, CancellationToken ct);
        Task<IEnumerable<DomainEvent>> GetEventsAsync(string streamId, CancellationToken ct);
    }
}
