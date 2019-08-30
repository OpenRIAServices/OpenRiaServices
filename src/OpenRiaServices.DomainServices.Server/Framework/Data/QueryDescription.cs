using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Represents a query operation to be processed by a <see cref="DomainService"/>
    /// </summary>
    public sealed class QueryDescription
    {
        private readonly DomainOperationEntry _domainOperationEntry;
        private object[] _parameterValues;
        private readonly IQueryable _query;
        private readonly bool _includeTotalCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryDescription"/> class with the specified
        /// <see cref="DomainOperationEntry"/>.
        /// </summary>
        /// <param name="domainOperationEntry">The query operation to be processed</param>
        public QueryDescription(DomainOperationEntry domainOperationEntry)
        {
            if (domainOperationEntry == null)
            {
                throw new ArgumentNullException("domainOperationEntry");
            }
            if (domainOperationEntry.Operation != DomainOperation.Query)
            {
                throw new ArgumentOutOfRangeException("domainOperationEntry");
            }

            this._domainOperationEntry = domainOperationEntry;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryDescription"/> class with the specified
        /// <see cref="DomainOperationEntry"/> and parameter values.
        /// </summary>
        /// <param name="domainOperationEntry">The query operation to be processed</param>
        /// <param name="parameterValues">Parameter values for the method if it requires any</param>
        public QueryDescription(DomainOperationEntry domainOperationEntry, object[] parameterValues) : this(domainOperationEntry)
        {
            if (parameterValues == null)
            {
                throw new ArgumentNullException("parameterValues");
            }

            this._parameterValues = parameterValues;
        }
       
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryDescription"/> class with the specified
        /// <see cref="DomainOperationEntry"/>, parameter values, flag indicating whether
        /// to evaluate and include total entity count in the result and (optional) query to compose over
        /// the results.
        /// </summary>
        /// <param name="domainOperationEntry">The query operation to be processed</param>
        /// <param name="parameterValues">Parameter values for the method if it requires any</param>
        /// <param name="includeTotalCount">Flag to indicate that total entity count is required</param>
        /// <param name="query">The query to compose over the results</param>
        public QueryDescription(DomainOperationEntry domainOperationEntry, object[] parameterValues, bool includeTotalCount, IQueryable query)
            : this(domainOperationEntry, parameterValues)
        {
            this._query = query;
            this._includeTotalCount = includeTotalCount;
        }

        /// <summary>
        /// Gets the query operation to be processed
        /// </summary>
        public DomainOperationEntry Method
        {
            get
            {
                return this._domainOperationEntry;
            }
        }

        /// <summary>
        /// Gets the parameter values for the method if it requires any
        /// </summary>
        public object[] ParameterValues
        {
            get
            {
                if (this._parameterValues == null)
                {
                    this._parameterValues = Array.Empty<object>();
                }
                return this._parameterValues;
            }
        }

        /// <summary>
        /// Gets The query to compose over the results
        /// </summary>
        public IQueryable Query
        {
            get
            {
                return this._query;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the total entity count needs to be automatically evaluated and included in the result.
        /// </summary>
        public bool IncludeTotalCount
        {
            get
            {
                return this._includeTotalCount;
            }
        }
    }
}
