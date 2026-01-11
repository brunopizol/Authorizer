using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Entities
{
    public record SlaViolation
    {
        public string TransactionId { get; init; } = string.Empty;
        public TimeSpan Duration { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
