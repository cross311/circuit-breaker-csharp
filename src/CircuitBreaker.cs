using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CircuitBreakerDotNet
{
    public class CircuitBreaker
    {
        private readonly int _FailLimit;

        private int _FailCount;
        private Exception _LastException;

        public CircuitBreaker(int failLimit)
        {
            _FailLimit = failLimit;
            _FailCount = 0;
        }

        public void ExecuteAction(Action action)
        {
            if (FailLimitReached())
            {
                throw CreateCircuitOpenException();
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref _FailCount);
                _LastException = exception;

                if (FailLimitReached())
                {
                    throw CreateCircuitOpenException();
                }

                throw;
            }
        }

        private CircuitBreakerOpenException CreateCircuitOpenException()
        {
            return new CircuitBreakerOpenException(_LastException);
        }

        private bool FailLimitReached()
        {
            var result = Interlocked.Equals(_FailCount, _FailLimit);
            return result;
        }
    }

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
