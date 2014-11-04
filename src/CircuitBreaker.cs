using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CircuitBreakerDotNet
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ICircuitBreakerStateStore _StateStore;
        private readonly ICircuitBreakerClosedToOpenStateTransition _ClosedToOpen;
        private readonly ICircuitBreakerOpenToClosedStateTransition _OpenToClosed;
        private readonly object _HalfOpenSyncObject;

        public CircuitBreaker(
            ICircuitBreakerStateStore stateStore,
            ICircuitBreakerClosedToOpenStateTransition closedToOpen,
            ICircuitBreakerOpenToClosedStateTransition openToClosed)
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
                OpenCircuitAction(action);
                return;
            }

            ClosedCircuitAction(action);
        }

        private void OpenCircuitAction(Action action)
        {
            if (IsReadyToHalfOpen() && HalfOpenningCircuitSucceeds(action))
            {
                return;
            }

            throw CreateCircuitOpenException();
        }

        private bool HalfOpenningCircuitSucceeds(Action action)
        {
            var lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(_HalfOpenSyncObject);
                if (lockTaken)
                {
                    _StateStore.HalfOpen();

                    action();

                    _StateStore.Reset();

                    return true;
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

            return false;
        }

        private bool IsReadyToHalfOpen()
        {
            var isReadyToHalfOpen = _OpenToClosed.CircuitOpenIsCircuitReadyToHalfOpen(_StateStore);
            return isReadyToHalfOpen;
        }

        private void ClosedCircuitAction(Action action)
        {
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
}
