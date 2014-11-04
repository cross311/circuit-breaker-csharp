using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{
    public enum CircuitBreakerStateEnum
    {
        Closed,
        Open,
        HalfOpen
    }
}
