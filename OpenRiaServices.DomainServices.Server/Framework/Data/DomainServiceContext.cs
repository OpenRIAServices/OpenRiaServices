using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Security.Principal;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Represents the execution context for a <see cref="DomainService"/> request.
    /// </summary>
    public class DomainServiceContext : IServiceProvider
    {
        private DomainOperationEntry _operation;
        private DomainOperationType _operationType;
        private IServiceContainer _serviceContainer;
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the DomainServiceContext class
        /// </summary>
        /// <param name="serviceProvider">A service provider.</param>
        /// <param name="operationType">The type of operation that is being executed.</param>
        public DomainServiceContext(IServiceProvider serviceProvider, DomainOperationType operationType)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            this._serviceProvider = serviceProvider;
            this._operationType = operationType;
        }

        /// <summary>
        /// Copy constructor that creates a new context of the specified type copying
        /// the rest of the context from the provided instance.
        /// </summary>
        /// <param name="serviceContext">The service context to copy from.</param>
        /// <param name="operationType">The type of operation that is being executed.</param>
        internal DomainServiceContext(DomainServiceContext serviceContext, DomainOperationType operationType)
        {
            if (serviceContext == null)
            {
                throw new ArgumentNullException("serviceContext");
            }
            this._serviceProvider = serviceContext._serviceProvider;
            this._operationType = operationType;
        }

        /// <summary>
        /// Gets the operation that is being executed.
        /// </summary>
        public DomainOperationEntry Operation
        {
            get
            {
                return this._operation;
            }
            internal set
            {
                this._operation = value;
            }
        }

        /// <summary>
        /// Gets the type of operation that is being executed.
        /// </summary>
        public DomainOperationType OperationType
        {
            get
            {
                return this._operationType;
            }
        }

        /// <summary>
        /// The user for this context instance.
        /// </summary>
        public IPrincipal User
        {
            get
            {
                return (IPrincipal)this._serviceProvider.GetService(typeof(IPrincipal));
            }
        }

        #region IServiceProvider Members

        /// <summary>
        /// See <see cref="IServiceProvider.GetService(Type)"/>.
        /// When the <see cref="ServiceContainer"/> is in use, it will be used
        /// first to retrieve the requested service.  If the <see cref="ServiceContainer"/>
        /// is not being used or it cannot resolve the service, then the
        /// <see cref="IServiceProvider"/> provided to this <see cref="DomainServiceContext"/>
        /// will be queried for the service type.
        /// </summary>
        /// <param name="serviceType">The type of the service needed.</param>
        /// <returns>An instance of that service or null if it is not available.</returns>
        public object GetService(Type serviceType)
        {
            object service = null;

            if (this._serviceContainer != null)
            {
                service = this._serviceContainer.GetService(serviceType);
            }

            if (service == null && this._serviceProvider != null)
            {
                service = this._serviceProvider.GetService(serviceType);
            }

            return service;
        }

        #endregion

        #region Service Container

        /// <summary>
        /// A <see cref="IServiceContainer"/> that can be used for adding,
        /// removing, and getting services during a domain service invocation. <see cref="GetService"/>
        /// will query into this container as well as the <see cref="IServiceProvider"/>
        /// specified in the constructor.
        /// </summary>
        /// <remarks>
        /// If the <see cref="IServiceProvider"/> specified to the constructor implements
        /// <see cref="IServiceContainer"/>, then it will be used as the
        /// <see cref="ServiceContainer"/>, otherwise an empty container will be initialized.
        /// </remarks>
        internal IServiceContainer ServiceContainer
        {
            get
            {
                if (this._serviceContainer == null)
                {
                    this._serviceContainer = new DomainServiceContextServiceContainer();
                }

                return this._serviceContainer;
            }
        }

        /// <summary>
        /// Private implementation of <see cref="IServiceContainer"/> to act
        /// as a default service container on <see cref="DomainServiceContext"/>.
        /// </summary>
        private class DomainServiceContextServiceContainer : IServiceContainer
        {
            #region Member Fields

            private readonly object _lock = new object();
            private IServiceContainer _parentContainer;
            private Dictionary<Type, object> _services = new Dictionary<Type, object>();

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a new service container that does not have a parent container
            /// </summary>
            internal DomainServiceContextServiceContainer()
            {
            }

            /// <summary>
            /// Constructs a new service container that has a parent container, making this container
            /// a wrapper around the parent container.  
            /// Calls to <see cref="AddService(Type, ServiceCreatorCallback, bool)"/> and <see cref="RemoveService(Type, bool)"/> 
            /// will promote to the parent container by default, unless the "promote" param of those methods is
            /// specified as <c>false</c> on those calls.
            /// </summary>
            /// <param name="parentContainer">The parent container to wrap into this container.</param>
            internal DomainServiceContextServiceContainer(IServiceContainer parentContainer)
            {
                this._parentContainer = parentContainer;
            }

            #endregion

            #region IServiceContainer Members

            /// <summary>
            /// Adds the specified service to the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to add.</param>
            /// <param name="callback">A callback object that is used to create the service. This allows a service to be declared as available, but delays the creation of the object until the service is requested.</param>
            /// <param name="promote"><value>true</value> to promote this request to any parent service containers; otherwise, <value>false</value>. </param>
            public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
            {
                if (promote && this._parentContainer != null)
                {
                    this._parentContainer.AddService(serviceType, callback, promote);
                }
                else
                {
                    lock (this._lock)
                    {
                        if (this._services.ContainsKey(serviceType))
                        {
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceContextServiceContainer_ItemAlreadyExists, serviceType), "serviceType");
                        }

                        this._services.Add(serviceType, callback);
                    }
                }
            }

            /// <summary>
            /// Adds the specified service to the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to add.</param>
            /// <param name="callback">A callback object that is used to create the service. This allows a service to be declared as available, but delays the creation of the object until the service is requested.</param>
            public void AddService(Type serviceType, ServiceCreatorCallback callback)
            {
                this.AddService(serviceType, callback, true);
            }

            /// <summary>
            /// Adds the specified service to the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to add.</param>
            /// <param name="serviceInstance">An instance of the service type to add. This object must implement or inherit from the type indicated by the <paramref name="serviceType"/> parameter.</param>
            /// <param name="promote"><value>true</value> to promote this request to any parent service containers; otherwise, <value>false</value>. </param>
            public void AddService(Type serviceType, object serviceInstance, bool promote)
            {
                if (promote && this._parentContainer != null)
                {
                    this._parentContainer.AddService(serviceType, serviceInstance, promote);
                }
                else
                {
                    lock (this._lock)
                    {
                        if (this._services.ContainsKey(serviceType))
                        {
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceContextServiceContainer_ItemAlreadyExists, serviceType), "serviceType");
                        }

                        this._services.Add(serviceType, serviceInstance);
                    }
                }
            }

            /// <summary>
            /// Adds the specified service to the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to add.</param>
            /// <param name="serviceInstance">An instance of the service type to add. This object must implement or inherit from the type indicated by the <paramref name="serviceType"/> parameter.</param>
            public void AddService(Type serviceType, object serviceInstance)
            {
                this.AddService(serviceType, serviceInstance, true);
            }

            /// <summary>
            /// Removes the specified service type from the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to remove.</param>
            /// <param name="promote"><value>true</value> to promote this request to any parent service containers; otherwise, <value>false</value>. </param>
            public void RemoveService(Type serviceType, bool promote)
            {
                lock (this._lock)
                {
                    if (this._services.ContainsKey(serviceType))
                    {
                        this._services.Remove(serviceType);
                    }
                }

                if (promote && this._parentContainer != null)
                {
                    this._parentContainer.RemoveService(serviceType);
                }
            }

            /// <summary>
            /// Removes the specified service type from the service container.
            /// </summary>
            /// <param name="serviceType">The type of service to remove.</param>
            public void RemoveService(Type serviceType)
            {
                this.RemoveService(serviceType, true);
            }

            #endregion

            #region IServiceProvider Members

            /// <summary>
            /// See <see cref="IServiceProvider.GetService(Type)"/>.
            /// </summary>
            /// <param name="serviceType">The type of the service needed.</param>
            /// <returns>An instance of that service or null if it is not available.</returns>
            public object GetService(Type serviceType)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }

                object service = null;
                this._services.TryGetValue(serviceType, out service);

                if (service == null && this._parentContainer != null)
                {
                    service = this._parentContainer.GetService(serviceType);
                }

                ServiceCreatorCallback callback = service as ServiceCreatorCallback;

                if (callback != null)
                {
                    service = callback(this, serviceType);
                }

                return service;
            }

            #endregion
        }

        #endregion
    }
}
