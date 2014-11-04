using System;
namespace CircuitBreakerDotNet
{
    /// <summary>
    /// Circuit Breaker Pattern
    /// Based off: http://msdn.microsoft.com/en-us/library/dn589784.aspx
    /// 
    /// Handle faults that may take a variable amount of time to rectify
    /// when connecting to a remote service or resource.
    /// This pattern can improve the stability and resiliency of
    /// an application.
    /// 
    /// Example:
    /// <code>
    /// var breaker = new CircuitBreaker();
    /// try
    /// {
    ///     breaker.ExecuteAction(() =>
    ///     {
    ///         // Operation protected by the circuit breaker.
    ///         ...
    ///     });
    /// }
    /// catch (CircuitBreakerOpenException ex)
    /// {
    ///     // Perform some different action when the breaker is open.
    ///     // Last exception details are in the inner exception.
    ///     ...
    /// }
    /// catch (Exception ex)
    /// {
    ///     ...
    /// }
    /// </code>
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Execute Action within
        /// a circuit breaker.
        /// Behavouir will be based on how
        /// the CircuitBreaker is built
        /// </summary>
        /// <param name="action">Action to be performed</param>
        void ExecuteAction(Action action);
    }
}
