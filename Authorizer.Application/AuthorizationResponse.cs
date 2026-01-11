using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Application
{
    public record AuthorizationResponse
    {
        public bool IsApproved { get; init; }
        public string? AuthorizationCode { get; init; }
        public string? DenialReason { get; init; }
        public string Message { get; init; } = string.Empty;
        public TimeSpan ProcessingTime { get; init; }
    }
}
