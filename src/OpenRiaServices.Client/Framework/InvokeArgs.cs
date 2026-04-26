using System;
using System.Collections.Generic;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents the information required to call an Invoke operation.
    /// </summary>
    public sealed class InvokeArgs
    {
        /// <summary>
        /// Initializes a new instance of the InvokeArgs class
        /// </summary>
        /// <param name="operationName">The name of the invoke operation.</param>
        /// <param name="returnType">The return Type of the invoke operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the method takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        public InvokeArgs(string operationName, Type returnType, IDictionary<string, object?>? parameters, bool hasSideEffects)
        {
            ArgumentException.ThrowIfNullOrEmpty(operationName);
            ArgumentNullException.ThrowIfNull(returnType);

            OperationName = operationName;
            ReturnType = returnType;
            Parameters = parameters;
            HasSideEffects = hasSideEffects;
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the return Type of the operation.
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// Optional parameters required by the operation. Returns null
        /// if the method takes no parameters.
        /// </summary>
        public IDictionary<string, object?>? Parameters { get; }

        /// <summary>
        /// Gets a value indicating whether the operation has side-effects.
        /// </summary>
        public bool HasSideEffects { get; }
    }
}
