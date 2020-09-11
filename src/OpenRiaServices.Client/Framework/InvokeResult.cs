using System;
using System.Linq;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Interface for non-generic access to <see cref="InvokeResult{T}"/>
    /// used by <see cref="InvokeOperation"/>
    /// </summary>
    interface IInvokeResult
    {
        object Value { get; }
    }

    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult : IInvokeResult
    {
        object IInvokeResult.Value => null;
    }

    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult<T> : InvokeResult, IInvokeResult
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeResult{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public InvokeResult(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value => _value;

        object IInvokeResult.Value => _value;

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
