using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CircuitBreakerDotNet
{

    public class FailCountBasedCircuitBreakerClosedToOpenStateTransition : ICircuitBreakerClosedToOpenStateTransition
    {
        private readonly int _FailLimit;
        private readonly TimeSpan _TimeToRememberFailures;

        private int _FailCount;
        private DateTime _LastFailTimeUTC;

        public FailCountBasedCircuitBreakerClosedToOpenStateTransition(int failLimit, TimeSpan timeToRememberFailures)
        {
            _FailLimit = failLimit;
            _TimeToRememberFailures = timeToRememberFailures;
            _FailCount = 0;
        }

        public bool ExceptionThrownWhileCircuitClosedShouldTripCircuit(Exception exceptionThrown, ICircuitBreakerState currentState)
        {
            Interlocked.Increment(ref _FailCount);
            var shouldTripCircuit = Interlocked.Equals(_FailCount, _FailLimit);
            _LastFailTimeUTC = DateTime.UtcNow;

            return shouldTripCircuit;
        }

        public void ActionExecutedSuccessfullyWhileCircuitClosed(ICircuitBreakerState currentState)
        {
            if (_FailCount == 0 || _LastFailTimeUTC + _TimeToRememberFailures > DateTime.UtcNow)
                return;

            Interlocked.Exchange(ref _FailCount, 0);
        }
    }
}
