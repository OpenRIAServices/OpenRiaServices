using System;
using System.Linq;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Interface for non-generic access to <see cref="InvokeResult{T}"/>
    /// used by <see cref="InvokeOperation"/>
    /// </summary>
    interface IInvokeResult
    {
        /// <summary>
        /// Get the value returned by the Invoke operation, if any.  If the Invoke operation does not return a value, this will be null.
        /// </summary>
        object? Value { get; }
    }

    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult : IInvokeResult
    {
        object? IInvokeResult.Value => null;
    }

    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult<T> : InvokeResult, IInvokeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeResult{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public InvokeResult(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; }

        object? IInvokeResult.Value => Value;

        /// <summary>
        /// Implicit conversion to <typeparamref name="T"/> so that <see cref="Value"/> does not need to be used
        /// in most cases.
        /// </summary>
        /// <param name="invokeResult"></param>
        public static implicit operator T(InvokeResult<T> invokeResult)
        {
            return invokeResult.Value;
        }
    }

}
