using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{

    public interface ICircuitBreakerClosedToOpenStateTransition
    {
        bool ExceptionThrownWhileCircuitClosedShouldTripCircuit(Exception exceptionThrown, ICircuitBreakerState currentState);
        void ActionExecutedSuccessfullyWhileCircuitClosed(ICircuitBreakerState currentState);
    }
}
