using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{

    public interface ICircuitBreakerStateStore : ICircuitBreakerState
    {
        void Trip(Exception exception);
        void Reset();
        void HalfOpen();
    }
}
