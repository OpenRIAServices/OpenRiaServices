using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents the result of a query from <see cref="DomainService.QueryAsync"/>
    /// Which is either an IEnumerable/IQueryable with optionally a specific count, or one or more validation errors
    /// </summary>
    public struct ServiceQueryResult<T>
    {
        /// <summary>
        /// Create a <see cref="ServiceQueryResult{T}"/> which represents the result of a successfully executed query
        /// </summary>
        /// <param name="result">the result of the query (before filtering from the client is applied)</param>
        /// <param name="totalResult">the total number of items for the type of entity returned by the query, or <c>null</c></param>
        public ServiceQueryResult(IEnumerable<T> result, int? totalResult)
        {
            Result = result;
            TotalCount = totalResult ?? DomainService.TotalCountUndefined;
            ValidationErrors = null;
        }

        /// <summary>
        /// Create a <see cref="ServiceQueryResult{T}"/> where there were validation errors which prevented a result from beein returned
        /// </summary>
        /// <param name="validationErrors">the validation errors, should <c>never</c> be modified after beeing passed in</param>
        public ServiceQueryResult(IReadOnlyCollection<ValidationResult> validationErrors)
        {
            if (validationErrors == null)
                throw new ArgumentNullException(nameof(validationErrors));
            if (validationErrors.Count == 0)
                throw new ArgumentException(Resource.ValidationErrorsCannotBeEmpty, nameof(validationErrors));

            ValidationErrors = validationErrors;
            TotalCount = DomainService.TotalCountUndefined;
            Result = null;
        }

        /// <summary>
        /// The result of the query, or <c>null</c> if <see cref="HasValidationErrors"/> is true
        /// </summary>
        public IEnumerable<T> Result { get; }

        /// <summary>
        /// Total number of items for the entity type returned, or any of the special values 
        /// <see cref="DomainService.TotalCountUndefined"/> or <see cref="DomainService.TotalCountEqualsResultSetCount"/>
        /// </summary>
        public int TotalCount { get; }

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
