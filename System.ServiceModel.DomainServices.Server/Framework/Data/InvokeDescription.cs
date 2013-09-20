using System.Collections.Generic;

namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Represents an invoke operation to be processed by a <see cref="DomainService"/>
    /// </summary>
    public sealed class InvokeDescription
    {
        private DomainOperationEntry _domainOperationEntry;
        private object[] _parameterValues;

        /// <summary>
        /// Initializes a new instance of the InvokeDescription class
        /// </summary>
        /// <param name="domainOperationEntry">The invoke operation to be processed</param>
        /// <param name="parameterValues">The parameter values for the method if it requires any.</param>
        public InvokeDescription(DomainOperationEntry domainOperationEntry, object[] parameterValues)
        {
            if (domainOperationEntry == null)
            {
                throw new ArgumentNullException("domainOperationEntry");
            }

            this._domainOperationEntry = domainOperationEntry;
            this._parameterValues = parameterValues;
        }

        /// <summary>
        /// Gets the invoke operation to be processed
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
                    this._parameterValues = new object[0];
                }
                return this._parameterValues;
            }
        }
    }
}
