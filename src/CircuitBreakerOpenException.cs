using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{
    public class CircuitBreakerOpenException : Exception
    {
        private readonly Exception _LastExceptionThatTrippedCircuit;

        public CircuitBreakerOpenException(Exception lastExceptionThatTrippedCircuit)
        {
            _LastExceptionThatTrippedCircuit = lastExceptionThatTrippedCircuit;
        }

        public Exception LastExceptionThatTrippedCircuit
        {
            get
            {
                return _LastExceptionThatTrippedCircuit;
            }
        }
    }
}
