using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{

    public interface ICircuitBreakerState
    {
        CircuitBreakerStateEnum State { get; }
        DateTime LastStateChangedDateUtc { get; }
        Exception LastException { get; }
        bool IsClosed { get; }
    }
}
