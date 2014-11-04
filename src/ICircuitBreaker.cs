using System;
namespace CircuitBreakerDotNet
{
    public interface ICircuitBreaker
    {
        void ExecuteAction(Action action);
    }
}
