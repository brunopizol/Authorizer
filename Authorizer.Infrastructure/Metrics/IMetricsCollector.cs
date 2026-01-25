using Authorizer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.Application.Metrics
{
    public interface IMetricsCollector
    {
        void RecordAuthorization(TimeSpan duration, bool approved);
        void RecordSlaViolation(string transactionId, TimeSpan duration);
        MetricsSnapshot GetSnapshot();
    }
}
