using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Class representing the result of a <see cref="DomainClient"/> operation.
    /// </summary>
    internal sealed class DomainClientResult
    {
        private DomainClientResult(object returnValue, IEnumerable<ValidationResult> validationErrors)
        {
            this.ReturnValue = returnValue;
            this.ValidationErrors = new ReadOnlyCollection<ValidationResult>(validationErrors.ToList());
        }

        /// <summary>
        /// Creates an Invoke operation result.
        /// </summary>
        /// <param name="returnValue">The return value of the Invoke operation.</param>
        /// <param name="validationErrors">Collection of validation errors for the invocation.</param>
        /// <returns>The result.</returns>
        public static DomainClientResult CreateInvokeResult(object returnValue, IEnumerable<ValidationResult> validationErrors)
        {
            if (validationErrors == null)
            {
                throw new ArgumentNullException(nameof(validationErrors));
            }
            return new DomainClientResult(returnValue, validationErrors);
        }

        /// <summary>
        /// Gets the return value of an Invoke operation. Can be null.
        /// </summary>
        public object ReturnValue { get; private set; }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationResult> ValidationErrors { get; }

        /// <summary>
        /// Gets the total server entity count for the original query without any paging applied to it.
        /// Automatic evaluation of the total server entity count requires the <see cref="EntityQuery.IncludeTotalCount"/>
        /// property to be set to <c>true</c>
        /// If the value is -1, the server did not support total-counts.
        /// </summary>
        public int TotalEntityCount { get; }
    }
}
