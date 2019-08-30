using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;

using DataAnnotationsResources = OpenRiaServices.DomainServices.Server.Resource;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Describes the context in which an authorization is being performed.
    /// </summary>
    /// <remarks>
    /// This class contains information describing the instance and the operation
    /// being authorized.  It implements <see cref="IDisposable"/> and must be
    /// properly disposed after use.
    /// <para>
    /// It supports <see cref="IServiceProvider"/> so that custom validation
    /// code can acquire additional services to help it perform its validation.
    /// </para>
    /// <para>
    /// An <see cref="Items"/> property bag is available for additional contextual
    /// information about the authorization.  Values stored in <see cref="Items"/>
    /// will be available to authorization methods that use this <see cref="AuthorizationContext"/>
    /// </para>
    /// <para>
    /// This class also provides an <see cref="IServiceContainer"/> implementation to allow
    /// developers to add services to the context at runtime.   This container is available
    /// via <see cref="GetService"/> specifying <c>typeof(IServiceContainer)</c> or via
    /// the <see cref="ServiceContainer"/> property.
    /// </para>
    /// </remarks>
    public sealed class AuthorizationContext : IServiceProvider, IDisposable
    {
        private object _instance;
        private string _operation;
        private string _operationType;
        private Dictionary<object, object> _items;
        private readonly IServiceProvider _parentServiceProvider;
        private ServiceContainer _serviceContainer;

        /// <summary>
        /// Initalizes a new instance of the <see cref="AuthorizationContext"/> class that can be used as a template.
        /// </summary>
        /// <remarks>
        /// This form of the contructor creates only a template <see cref="AuthorizationContext"/>
        /// which cannot be used directly in authorization requests.
        /// <para>
        /// A template <see cref="AuthorizationContext"/> is one which has been configured with
        /// a set of services and <see cref="Items"/> the developer wants to use for authorization
        /// requests.  It cannot be used directly during authorization, but an alternate form of the 
        /// constructor allows other <see cref="AuthorizationContext"/> instances to clone that
        /// template's state.
        /// </para>
        /// <para>
        /// The <see cref="OpenRiaServices.DomainServices.Server.DomainService.AuthorizationContext"/> 
        /// property allows such a template to be set into the
        /// <see cref="OpenRiaServices.DomainServices.Server.DomainService"/> for all authorization requests.
        /// </para>
        /// </remarks>
        /// <param name="serviceProvider">Optional parent <see cref="IServiceProvider"/> to which calls to
        /// <see cref="GetService"/> can be delegated.</param>
        public AuthorizationContext(IServiceProvider serviceProvider)
        {
            this._parentServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="AuthorizationContext"/> class that can be used for authorization.
        /// </summary>
        /// <param name="instance">Optional object instance.</param>
        /// <param name="operation">Name of the operation requiring authorization, such as "GetEmployees".</param>
        /// <param name="operationType">Kind of the operation requiring authorization, such as "Query".</param>
        /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> to use when <see cref="GetService"/> is called.
        /// </param>
        /// <param name="items">Optional set of key/value pairs to make available to consumers via <see cref="Items"/>.
        /// If null, an empty dictionary will be created.  If not null, the set of key/value pairs will be copied into a
        /// new dictionary, preventing consumers from modifying the original dictionary.
        /// </param>
        /// <exception cref="ArgumentNullException">When <paramref name="operation"/> or <paramref name="operationType"/> is <c>null</c> or empty.</exception>
        public AuthorizationContext(object instance, string operation, string operationType, IServiceProvider serviceProvider, IDictionary<object, object> items) : this(serviceProvider)
        {
            this.Setup(instance, operation, operationType, items);
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="AuthorizationContext"/> class from a template that can be used for authorization.
        /// </summary>
        /// <remarks>
        /// The specified <paramref name="authorizationContext"/> will be used as the new instance's
        /// <see cref="IServiceProvider"/>, and a snapshot of its <see cref="Items"/> will be captured.
        /// </remarks>
        /// <param name="instance">Optional object instance.</param>
        /// <param name="operation">Name of the operation requiring authorization, such as "GetEmployees".</param>
        /// <param name="operationType">Kind of the operation requiring authorization, such as "Query".</param>
        /// <param name="authorizationContext">An existing <see cref="AuthorizationContext"/> to use as a template.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="operation"/> or <paramref name="operationType"/> is <c>null</c> or empty
        /// or <paramref name="authorizationContext"/> is <c>null</c>.</exception>
        public AuthorizationContext(object instance, string operation, string operationType, AuthorizationContext authorizationContext)
            : this((IServiceProvider) authorizationContext)
        {
            if (authorizationContext == null)
            {
                throw new ArgumentNullException(nameof(authorizationContext));
            }

            // We use the _items field rather than the property to preserve the lazy instantiation semantics if it's null
            this.Setup(instance, operation, operationType, authorizationContext._items);
        }

        /// <summary>
        /// Gets a <see cref="Design.IServiceContainer"/> that can be used for adding,
        /// removing, and getting services used for authorization.  <see cref="GetService"/>
        /// will query into this container as well as the <see cref="IServiceProvider"/>
        /// specified in the constructor.
        /// </summary>
        /// <value>
        /// This container will be created only on demand and is effectively scoped
        /// to this <see cref="AuthorizationContext"/> instance.  It will not promote
        /// add or remove requests.
        /// </value>
        public IServiceContainer ServiceContainer
        {
            get
            {
                if (this._serviceContainer == null)
                {
                    this._serviceContainer = new ServiceContainer();
                }
                return this._serviceContainer;
            }
        }

        /// <summary>
        /// Gets the object instance being authorized.
        /// </summary>
        /// <value>This value may be <c>null</c> in situations where no object instance is available, 
        /// such as when authorizing queries or evaluating whether an operation can be attempted.
        /// <para>
        /// Subclasses of <see cref="AuthorizationAttribute"/> that depend on this value to
        /// perform instance-level authorization should check for <c>null</c>, and if everything
        /// else is acceptable, they should allow the authorization request.
        /// </para>
        /// </value>
        public object Instance
        {
            get
            {
                // This value is unconditionally off limits for a "template" AuthorizationContext
                this.EnsureNotTemplate();
                return this._instance;
            }
        }

        /// <summary>
        /// Gets the dictionary of key/value pairs associated with this context.
        /// </summary>
        /// <value>This property will never return <c>null</c>, but the dictionary may be empty.  Changes made
        /// to items in this dictionary will never affect the original dictionary specified in the constructor.</value>
        public IDictionary<object, object> Items
        {
            get
            {
                if (this._items == null)
                {
                    this._items = new Dictionary<object, object>();
                }
                return this._items;
            }
        }

        /// <summary>
        /// Gets the name of the operation being authorized.
        /// </summary>
        /// <value>This value will never be null or empty.  It reflects
        /// the developer's operation name, such as "GetEmployees".</value>
        public string Operation
        {
            get
            {
                // This value is unconditionally off limits for a "template" AuthorizationContext
                this.EnsureNotTemplate();
                return this._operation;
            }
        }

        /// <summary>
        /// Gets the kind of the operation being authorized.
        /// </summary>
        /// <value>This value will never be null or empty.  It reflects the category of
        /// the operation being authorized.  Example values are "Query" and "Invoke".</value>
        public string OperationType
        {
            get
            {
                // This value is unconditionally off limits for a "template" AuthorizationContext
                this.EnsureNotTemplate();
                return this._operationType;
            }
        }

        /// <summary>
        /// Helper method that throws <see cref="InvalidOperationException"/> if the current
        /// instance is only a template.
        /// </summary>
        private void EnsureNotTemplate()
        {
            if (this._operation == null)
            {
                throw new InvalidOperationException(DataAnnotationsResources.AuthorizationContext_Template_Only);
            }
        }

        /// <summary>
        /// Helper method to initialize some ctor parameters
        /// </summary>
        /// <param name="instance">Optional instance.</param>
        /// <param name="operation">Required operation name.</param>
        /// <param name="operationType">Required operation type.</param>
        /// <param name="items">Optional name/value pairs.</param>
        private void Setup(object instance, string operation, string operationType, IDictionary<object, object> items)
        {
            // The instance will be null in situations such as query methods, so it is optional
            this._instance = instance;

            // Operation is required
            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentNullException(nameof(operation));
            }
            this._operation = operation;

            // OperationType is required
            if (string.IsNullOrEmpty(operationType))
            {
                throw new ArgumentNullException(nameof(operationType));
            }
            this._operationType = operationType;

            // Snapshot the dictionary if provided, else create lazily on demand
            if (items != null && items.Count != 0)
            {
                this._items = new Dictionary<object, object>(items);
            }
        }

        #region IDisposable methods

        /// <summary>
        /// Dispose this <see cref="AuthorizationContext"/>.
        /// </summary>
        public void Dispose()
        {
            // Developer remarks: this Dispose is adequate.  We do not implement the full Dispose
            // pattern because this class is (a) sealed, and (b) has no finalizer.
            // This method is idempotent.
            ServiceContainer serviceContainer = this._serviceContainer;
            this._serviceContainer = null;
            if (serviceContainer != null)
            {
                serviceContainer.Dispose();
            }
        }

        #endregion

        #region IServiceProvider methods
        /// <summary> 
        /// Returns a service of the specified <paramref name="serviceType"/>.
        /// </summary>
        /// <remarks>
        /// See <see cref="IServiceProvider.GetService(Type)"/>.
        /// </remarks>
        /// <param name="serviceType">The type of the service needed.</param>
        /// <returns>An instance of that service or null if it is not available.</returns>
        public object GetService(Type serviceType)
        {
            // Request for service container creates one if it does not already exist.
            if (serviceType == typeof(IServiceContainer))
            {
                return this.ServiceContainer;
            }

            // Use field not property so we delegate to container only if someone has
            // asked for it in the past (which created one).  By default, there is no service container.
            if (this._serviceContainer != null)
            {
                object service = this._serviceContainer.GetService(serviceType);
                if (service != null)
                {
                    return service;
                }
            }

            if (this._parentServiceProvider != null)
            {
                return this._parentServiceProvider.GetService(serviceType);
            }

            return null;
        }
        #endregion // IServiceProvider methods
    }
}
