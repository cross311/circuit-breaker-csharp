using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitBreakerDotNet
{

    public class CircuitBreakerStateStore : ICircuitBreakerStateStore
    {
        private CircuitBreakerStateEnum _State;
        private Exception _LastException;
        private DateTime _LastStateChangedDateUtc;

        public CircuitBreakerStateStore()
        {
            _State = CircuitBreakerStateEnum.Closed;
            _LastException = null;
            _LastStateChangedDateUtc = DateTime.MinValue;
        }


        public CircuitBreakerStateEnum State
        {
            get { return _State; }
        }

        public DateTime LastStateChangedDateUtc
        {
            get { return _LastStateChangedDateUtc; }
        }

        public Exception LastException
        {
            get { return _LastException; }
        }

        public bool IsClosed
        {
            get { return _State == CircuitBreakerStateEnum.Closed; }
        }

        public void Trip(Exception exception)
        {
            _State = CircuitBreakerStateEnum.Open;
            _LastException = exception;
            _LastStateChangedDateUtc = DateTime.UtcNow;
        }

        public void Reset()
        {
            _State = CircuitBreakerStateEnum.Closed;
            _LastException = null;
            _LastStateChangedDateUtc = DateTime.UtcNow;
        }

        public void HalfOpen()
        {
            _State = CircuitBreakerStateEnum.HalfOpen;
            _LastStateChangedDateUtc = DateTime.UtcNow;
        }
    }
}
