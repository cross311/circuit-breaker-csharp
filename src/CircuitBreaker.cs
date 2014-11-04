using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CircuitBreakerDotNet
{
    public enum CircuitBreakerStateEnum
    {
        Closed,
        Open,
        HalfOpen
    }

    public interface ICircuitBreakerState
    {
        CircuitBreakerStateEnum State { get; }
        DateTime LastStateChangedDateUtc { get; }
        Exception LastException { get; }
        bool IsClosed { get; }
    }

    public interface ICircuitBreakerStateStore : ICircuitBreakerState
    {
        void Trip(Exception exception);
        void Reset();
        void HalfOpen();

    }

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

    public interface ICircuitBreakerClosedToOpen
    {
        bool ExceptionThrownWhileCircuitClosedShouldTripCircuit(Exception exceptionThrown, ICircuitBreakerState currentState);
        void ActionExecutedSuccessfullyWhileCircuitClosed(ICircuitBreakerState currentState);
    }

    public class FailCountBasedCircuitBreakerClosedToOpen : ICircuitBreakerClosedToOpen
    {
        private readonly int _FailLimit;
        private readonly TimeSpan _TimeToRememberFailures;

        private int _FailCount;
        private DateTime _LastFailTimeUTC;

        public FailCountBasedCircuitBreakerClosedToOpen(int failLimit, TimeSpan timeToRememberFailures)
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

    public interface ICircuitBreakerOpenToClosed
    {
        bool CircuitOpenIsCircuitReadyToHalfOpen(ICircuitBreakerState currentState);
    }

    public class WaitTimeBasedCircuitBreakerOpenToClosed : ICircuitBreakerOpenToClosed
    {
        private readonly TimeSpan _OpenRetryWaitTime;

        public WaitTimeBasedCircuitBreakerOpenToClosed(TimeSpan openRetryWaitTime)
        {
            _OpenRetryWaitTime = openRetryWaitTime;
        }

        public bool CircuitOpenIsCircuitReadyToHalfOpen(ICircuitBreakerState currentState)
        {
            var isReadyToHalfOpen = currentState.LastStateChangedDateUtc + _OpenRetryWaitTime < DateTime.UtcNow;
            return isReadyToHalfOpen;
        }
    }

    public class CircuitBreaker
    {
        private readonly ICircuitBreakerStateStore _StateStore;
        private readonly ICircuitBreakerClosedToOpen _ClosedToOpen;
        private readonly ICircuitBreakerOpenToClosed _OpenToClosed;
        private readonly object _HalfOpenSyncObject;

        public CircuitBreaker(ICircuitBreakerStateStore stateStore, ICircuitBreakerClosedToOpen closedToOpen, ICircuitBreakerOpenToClosed openToClosed)
        {
            _StateStore = stateStore;
            _ClosedToOpen = closedToOpen;
            _OpenToClosed = openToClosed;
            _HalfOpenSyncObject = new object();
        }

        public void ExecuteAction(Action action)
        {
            if (IsOpen())
            {
                if (IsReadyToHalfOpen())
                {
                    var lockTaken = false;
                    try
                    {
                        lockTaken = Monitor.TryEnter(_HalfOpenSyncObject);
                        if (lockTaken)
                        {
                            action();

                            _StateStore.Reset();
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        _StateStore.Trip(exception);
                        throw;
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(_HalfOpenSyncObject);
                        }
                    }
                }
                throw CreateCircuitOpenException();
            }

            try
            {
                action();
                _ClosedToOpen.ActionExecutedSuccessfullyWhileCircuitClosed(_StateStore);
            }
            catch (Exception exception)
            {
                TrackException(exception);

                throw;
            }
        }

        private bool IsReadyToHalfOpen()
        {
            var isReadyToHalfOpen = _OpenToClosed.CircuitOpenIsCircuitReadyToHalfOpen(_StateStore);
            return isReadyToHalfOpen;
        }

        private void TrackException(Exception exception)
        {
            var shouldTripCiruit = ExceptionThrownWhileCircuitClosedShouldTripCircuit(exception);

            if (shouldTripCiruit)
            {
                _StateStore.Trip(exception);
            }
        }

        private bool ExceptionThrownWhileCircuitClosedShouldTripCircuit(Exception exception)
        {
            var shouldTripCircuit = _ClosedToOpen.ExceptionThrownWhileCircuitClosedShouldTripCircuit(exception, _StateStore);
            return shouldTripCircuit;
        }

        private bool IsOpen()
        {
            return !_StateStore.IsClosed;
        }

        private CircuitBreakerOpenException CreateCircuitOpenException()
        {
            return new CircuitBreakerOpenException(_StateStore.LastException);
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
