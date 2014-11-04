using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CircuitBreakerDotNet;
using System.Threading;

namespace CircuitBreakerTest
{
    [TestClass]
    public class CircuitBreakerShould
    {
        static int _FailLimit = 2;
        static TimeSpan _WaitTime = new TimeSpan(0, 0, 0, 0, 3);
        ICircuitBreakerStateStore _CircuitBreakerStateStore;
        ICircuitBreaker _CircuitBreaker;

        [TestInitialize]
        public void TestInitialize()
        {
            _CircuitBreakerStateStore = new CircuitBreakerStateStore();
            var closedToOpen = new FailCountBasedCircuitBreakerClosedToOpenStateTransition(_FailLimit, _WaitTime);
            var openToClosed = new WaitTimeBasedCircuitBreakerOpenToClosedStateTransition(_WaitTime);
            _CircuitBreaker = new CircuitBreaker(_CircuitBreakerStateStore, closedToOpen, openToClosed);
        }

        [TestMethod]
        public void ExecuteActionLikeNormal()
        {
            var wasExecuted = false;

            _CircuitBreaker.ExecuteAction(() =>
            {
                wasExecuted = true;
            });

            Assert.IsTrue(wasExecuted);
        }

        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public void ThrowActionsException()
        {
            _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
        }

        [TestMethod]
        public void ThrowCircuitBreakerOpenExceptionWhenActionFailsOverFailLimit()
        {

            for (var i = 0; i < _FailLimit; i++)
            {
                try
                {
                    _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
                }
                catch (DivideByZeroException) { }
            }

            try
            {
                _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
                Assert.Fail("Should have thrown CircuitBreakerOpenException");
            }
            catch (CircuitBreakerOpenException exception)
            {
                Assert.IsInstanceOfType(exception.LastExceptionThatTrippedCircuit, typeof(DivideByZeroException));
            }
        }

        [TestMethod]
        public void ThrowCircuitBreakerOpenExceptionWithoutExecutingActionWhenCircuitOpen()
        {
            var executed = false;

            MoveCircuitBreakerToOpenState();

            try
            {
                _CircuitBreaker.ExecuteAction(() => executed = true);
                Assert.Fail("Should have thrown CircuitBreakerOpenException");
            }
            catch (CircuitBreakerOpenException) { }

            Assert.IsFalse(executed);
        }

        [TestMethod]
        public void RetryActionAfterBeingOpenForWaitTimeAndCloseIfSuccessful()
        {
            MoveCircuitBreakerToOpenState();
            Thread.Sleep(_WaitTime.Add(new TimeSpan(0,0,0,0,3)));

            var tried = false;
            _CircuitBreaker.ExecuteAction(() => tried = true);

            Assert.IsTrue(tried);
            Assert.AreEqual(CircuitBreakerStateEnum.Closed, _CircuitBreakerStateStore.State);
        }

        [TestMethod]
        public void RetryActionAfterBeingOpenForWaitTimeAndGoBackToOpenIfFailed()
        {
            MoveCircuitBreakerToOpenState();
            Thread.Sleep(_WaitTime.Add(new TimeSpan(0, 0, 0, 0, 3)));

            try
            {
                _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
                Assert.Fail("Should have thrown DivideByZeroException");
            }
            catch (DivideByZeroException) { }

            Assert.AreEqual(CircuitBreakerStateEnum.Open, _CircuitBreakerStateStore.State);
        }



        [TestMethod]
        public void ForgetFailuresAfterAConfiguredTimeWhenSuccessHappens()
        {
            // Get right up to the fail limit
            for (var i = 0; i < (_FailLimit - 1); i++)
            {
                try
                {
                    _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
                }
                catch (DivideByZeroException) { }
            }

            // sleep some time
            Thread.Sleep(_WaitTime.Add(new TimeSpan(0, 0, 0, 0, 3)));

            // success
            _CircuitBreaker.ExecuteAction(() => { });

            try
            {
                _CircuitBreaker.ExecuteAction(DivideByZeroExceptionAction);
                Assert.Fail("Should have thrown DivideByZeroException");
            }
            catch (DivideByZeroException) { }

            Assert.AreEqual(CircuitBreakerStateEnum.Closed, _CircuitBreakerStateStore.State);
        }

        private static void DivideByZeroExceptionAction()
        {
            var zero = 0;
            zero = zero / zero;
        }

        private void MoveCircuitBreakerToOpenState()
        {
            _CircuitBreakerStateStore.Trip(new DivideByZeroException());
        }
    }
}
