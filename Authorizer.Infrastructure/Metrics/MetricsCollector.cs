using Authorizer.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Application.Metrics
{
    public class MetricsCollector : IMetricsCollector
    {
        private long _totalRequests;
        private long _approvedCount;
        private long _deniedCount;
        private long _slaViolations;
        private readonly ConcurrentBag<TimeSpan> _durations = new();
        private readonly ConcurrentBag<SlaViolation> _violations = new();

        public void RecordAuthorization(TimeSpan duration, bool approved)
        {
            Interlocked.Increment(ref _totalRequests);

            if (approved)
                Interlocked.Increment(ref _approvedCount);
            else
                Interlocked.Increment(ref _deniedCount);

            _durations.Add(duration);
        }

        public void RecordSlaViolation(string transactionId, TimeSpan duration)
        {
            Interlocked.Increment(ref _slaViolations);
            _violations.Add(new SlaViolation
            {
                TransactionId = transactionId,
                Duration = duration,
                Timestamp = DateTime.UtcNow
            });
        }

        public MetricsSnapshot GetSnapshot()
        {
            var durations = _durations.ToArray();

            return new MetricsSnapshot
            {
                TotalRequests = _totalRequests,
                ApprovedCount = _approvedCount,
                DeniedCount = _deniedCount,
                SlaViolations = _slaViolations,
                ApprovalRate = _totalRequests > 0 ? (double)_approvedCount / _totalRequests : 0,
                SlaComplianceRate = _totalRequests > 0 ? 1 - ((double)_slaViolations / _totalRequests) : 1,
                AverageDuration = durations.Any() ? TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)) : TimeSpan.Zero,
                P95Duration = CalculatePercentile(durations, 0.95),
                P99Duration = CalculatePercentile(durations, 0.99),
                RecentViolations = _violations.OrderByDescending(v => v.Timestamp).Take(10).ToList()
            };
        }

        private static TimeSpan CalculatePercentile(TimeSpan[] durations, double percentile)
        {
            if (!durations.Any()) return TimeSpan.Zero;

            var sorted = durations.OrderBy(d => d.TotalMilliseconds).ToArray();
            var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

}
