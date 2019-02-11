using System;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult
    {

    }

    /// <summary>
    /// The value of a successfully completed Invoke operation
    /// </summary>
    public class InvokeResult<T> : InvokeResult
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
        public T Value
        {
            get { return _value; }
        }

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
