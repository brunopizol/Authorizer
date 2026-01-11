using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Domain.Entities
{
    public record MetricsSnapshot
    {
        public long TotalRequests { get; init; }
        public long ApprovedCount { get; init; }
        public long DeniedCount { get; init; }
        public long SlaViolations { get; init; }
        public double ApprovalRate { get; init; }
        public double SlaComplianceRate { get; init; }
        public TimeSpan AverageDuration { get; init; }
        public TimeSpan P95Duration { get; init; }
        public TimeSpan P99Duration { get; init; }
        public List<SlaViolation> RecentViolations { get; init; } = new();
    }
}
