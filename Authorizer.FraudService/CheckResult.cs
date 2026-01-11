using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorizer.FraudService
{
    public record CheckResult
    {
        public bool Passed { get; init; }
        public string Reason { get; init; } = string.Empty;
        public int Weight { get; init; }

        public static CheckResult Success() =>
            new() { Passed = true, Weight = 0 };
    }

}
