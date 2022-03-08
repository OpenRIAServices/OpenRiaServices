using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Security.Principal;
using System.Threading;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents the execution context for a <see cref="DomainService"/> request.
    /// </summary>
    public class DomainServiceContext : IServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the DomainServiceContext class
        /// </summary>
        /// <param name="serviceProvider">A service provider.</param>
        /// <param name="user">The user calling the operation.</param>
        /// <param name="operationType">The type of operation that is being executed.</param>
        public DomainServiceContext(IServiceProvider serviceProvider, IPrincipal user, DomainOperationType operationType)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            this._serviceProvider = serviceProvider;
            this.OperationType = operationType;
            this.User = user;
        }

        /// <summary>
        /// OBSOLETE: Use other constructor
        /// </summary>
        [Obsolete("Use other constructor accepting a User, this will be removed in a future version")]
        public DomainServiceContext(IServiceProvider serviceProvider, DomainOperationType operationType)
            : this(serviceProvider, (IPrincipal)serviceProvider.GetService(typeof(IPrincipal)), operationType)
        {
        }

        /// <summary>
        /// Gets the operation that is being executed.
        /// </summary>
        public DomainOperationEntry Operation { get; internal set; }

        /// <summary>
        /// Gets the type of operation that is being executed.
        /// </summary>
        public DomainOperationType OperationType { get; }

        /// <summary>
        /// The user for this context instance.
        /// </summary>
        public IPrincipal User { get; }

        /// <summary>
        /// <see cref="CancellationToken"/> which may be used by hosting layer to request cancellation.
        /// </summary>
        public CancellationToken CancellationToken { get; protected set; }

        #region IServiceProvider Members

        /// <summary>
        /// See <see cref="IServiceProvider.GetService(Type)"/>. the
        /// <see cref="IServiceProvider"/> provided to this <see cref="DomainServiceContext"/>
        /// will be queried for the service type.
        /// </summary>
        /// <param name="serviceType">The type of the service needed.</param>
        /// <returns>An instance of that service or null if it is not available.</returns>
        public virtual object GetService(Type serviceType)
        {
            return this._serviceProvider?.GetService(serviceType);
        }

        #endregion
    }
}
