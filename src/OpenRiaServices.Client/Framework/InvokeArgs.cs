﻿using System;
using System.Collections.Generic;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents the information required to call an Invoke operation.
    /// </summary>
    public sealed class InvokeArgs
    {
        private readonly string _operationName;
        private readonly Type _returnType;
        private readonly IDictionary<string, object> _parameters;
        private readonly bool _hasSideEffects;

        /// <summary>
        /// Initializes a new instance of the InvokeArgs class
        /// </summary>
        /// <param name="operationName">The name of the invoke operation.</param>
        /// <param name="returnType">The return Type of the invoke operation.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the method takes no parameters.</param>
        /// <param name="hasSideEffects">True if the operation has side-effects, false otherwise.</param>
        public InvokeArgs(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentNullException(nameof(operationName));
            }
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            this._operationName = operationName;
            this._returnType = returnType;
            this._parameters = parameters;
            this._hasSideEffects = hasSideEffects;
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName
        {
            get 
            { 
                return this._operationName; 
            }
        }

        /// <summary>
        /// Gets the return Type of the operation.
        /// </summary>
        public Type ReturnType
        {
            get 
            {
                return this._returnType; 
            }
        }

        /// <summary>
        /// Optional parameters required by the operation. Returns null
        /// if the method takes no parameters.
        /// </summary>
        public IDictionary<string, object> Parameters
        {
            get 
            {
                return this._parameters; 
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operation has side-effects.
        /// </summary>
        public bool HasSideEffects
        {
            get 
            {
                return this._hasSideEffects; 
            }
        }
    }
}
