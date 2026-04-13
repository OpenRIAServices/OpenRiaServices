using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents the result of an invoke operation.
    /// </summary>
    public class InvokeCompletedResult
    {

        /// <summary>
        /// Initializes a new instance of the InvokeCompletedResult class
        /// </summary>
        /// <param name="returnValue">The value returned from the server.</param>
        public InvokeCompletedResult(object? returnValue)
            : this(returnValue, Array.Empty<ValidationResult>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvokeCompletedResult class
        /// </summary>
        /// <param name="returnValue">The value returned from the server.</param>
        /// <param name="validationErrors">A collection of validation errors.</param>
        public InvokeCompletedResult(object? returnValue, IEnumerable<ValidationResult> validationErrors)
        {
            if (validationErrors == null)
            {
                throw new ArgumentNullException(nameof(validationErrors));
            }

            ReturnValue = returnValue;
            ValidationErrors = validationErrors.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationResult> ValidationErrors { get; }

        /// <summary>
        /// Gets the value returned from the server.
        /// </summary>
        public object? ReturnValue { get; }
    }
}
