using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CircuitBreakerDotNet;

namespace CircuitBreakerTest
{
    [TestClass]
    public class CircuitBreakerShould
    {
        int _FailLimit = 2;
        CircuitBreaker _CircuitBreaker;

        [TestInitialize]
        public void TestInitialize()
        {
            _CircuitBreaker = new CircuitBreaker(_FailLimit);
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
            _CircuitBreaker.ExecuteAction(() =>
            {
                var zero = 0;
                zero = zero / zero;
            });
        }

        [TestMethod]
        public void ThrowCircuitBreakerOpenExceptionWhenActionFailsOverFailLimit()
        {
            Action divideByZeroAction = () =>
                {
                    var zero = 0;
                    zero = zero / zero;
                };
            for (var i = 0; i < (_FailLimit - 1); i++)
            {
                try
                {
                    _CircuitBreaker.ExecuteAction(divideByZeroAction);
                }
                catch (DivideByZeroException) { }
            }

            try
            {
                _CircuitBreaker.ExecuteAction(divideByZeroAction);
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

            Action divideByZeroAction = () =>
            {
                var zero = 0;
                zero = zero / zero;
            };
            for (var i = 0; i < (_FailLimit); i++)
            {
                try
                {
                    _CircuitBreaker.ExecuteAction(divideByZeroAction);
                }
                catch (DivideByZeroException) { }
                catch (CircuitBreakerOpenException exception)
                {
                    Assert.IsInstanceOfType(exception.LastExceptionThatTrippedCircuit, typeof(DivideByZeroException));
                }
            }

            try
            {
                _CircuitBreaker.ExecuteAction(() => executed = true);
                Assert.Fail("Should have thrown CircuitBreakerOpenException");
            }
            catch (CircuitBreakerOpenException) { }

            Assert.IsFalse(executed);
        }
    }
}
