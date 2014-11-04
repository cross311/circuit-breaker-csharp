using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{
    public interface ICircuitBreakerOpenToClosedStateTransition
    {
        bool CircuitOpenIsCircuitReadyToHalfOpen(ICircuitBreakerState currentState);
    }
}
