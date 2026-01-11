using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Events
{
    public abstract class DomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        
        public string EventType { get; init; }
        
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        
        public string AggregateId { get; init; } = string.Empty;
        
        public int Version { get; init; }

        protected DomainEvent()
        {
            EventType = GetEventTypeName();
        }

        private string GetEventTypeName()
        {
            var typeName = GetType().Name;
            
            if (typeName.EndsWith("Event"))
            {
                typeName = typeName[..^5];
            }

            return System.Text.RegularExpressions.Regex.Replace(typeName, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
        }
    }

}
