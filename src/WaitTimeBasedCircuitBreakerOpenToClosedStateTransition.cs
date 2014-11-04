using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{

    public class WaitTimeBasedCircuitBreakerOpenToClosedStateTransition : ICircuitBreakerOpenToClosedStateTransition
    {
        private readonly TimeSpan _OpenRetryWaitTime;

        public WaitTimeBasedCircuitBreakerOpenToClosedStateTransition(TimeSpan openRetryWaitTime)
        {
            _OpenRetryWaitTime = openRetryWaitTime;
        }

        public bool CircuitOpenIsCircuitReadyToHalfOpen(ICircuitBreakerState currentState)
        {
            var isReadyToHalfOpen = currentState.LastStateChangedDateUtc + _OpenRetryWaitTime < DateTime.UtcNow;
            return isReadyToHalfOpen;
        }
    }
}
