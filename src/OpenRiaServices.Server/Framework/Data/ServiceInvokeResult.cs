using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents the result of a query from <see cref="DomainService.InvokeAsync"/>
    /// Which is either the return value, or one or more validation errors
    /// </summary>
    public struct ServiceInvokeResult
    {
        /// <summary>
        /// Create a <see cref="ServiceInvokeResult"/> with the result of an successfully invoked Invoke operation
        /// </summary>
        /// <param name="result">the result from the invoked operation (or <c>null</c> if it was a void method)</param>
        public ServiceInvokeResult(object result)
        {
            Result = result;
            ValidationErrors = null;
        }

        /// <summary>
        /// Create a <see cref="ServiceInvokeResult"/> where there were validation errors which prevented a result from beein returned
        /// </summary>
        /// <param name="validationErrors">the validation errors, should <c>never</c> be modified after beeing passed in</param>
        public ServiceInvokeResult(IReadOnlyCollection<ValidationResult> validationErrors)
        {
            if (validationErrors == null)
                throw new ArgumentNullException(nameof(validationErrors));
            if (validationErrors.Count == 0)
                throw new ArgumentException(Resource.ValidationErrorsCannotBeEmpty, nameof(validationErrors));

            ValidationErrors = validationErrors;
            Result = null;
        }

        /// <summary>
        /// The result of the invocation, or <c>null</c> if <see cref="HasValidationErrors"/> is true
        /// </summary>
        public object Result { get; }

        /// <summary>
        /// <c>true</c> if the query unsuccessfull and <see cref="ValidationErrors"/> contains the errors.
        /// <c>false</c> means that the query was successfull with results in <see cref="Result"/>
        /// </summary>
        public bool HasValidationErrors => ValidationErrors != null;

        /// <summary>
        /// Gets the validation erros if any, or <c>null</c> if there were no validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationResult> ValidationErrors { get; }
    }
}
