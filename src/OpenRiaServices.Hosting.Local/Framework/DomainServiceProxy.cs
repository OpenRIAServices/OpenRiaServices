using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using OpenRiaServices.Server;
using System.Web;

namespace OpenRiaServices.Hosting.Local
{
    /// <summary>
    /// Provides a set of methods that create <see cref="DomainService"/> proxies intended
    /// for use in in-process scenarios.
    /// </summary>
    public static class DomainServiceProxy
    {
        #region Fields and Delegates

        /// <summary>
        /// Delegate used to pass proxies a reference to the <see cref="DomainServiceProxyHelper.Query"/> method.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="queryName">The name of the query to invoke.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The query results. May be null if there are no query results.</returns>
        internal delegate IEnumerable QueryHelperDelegate(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string queryName, object[] parameters);

        /// <summary>
        /// Delegate used to pass proxies a reference to the <see cref="DomainServiceProxyHelper.Invoke"/> method.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="name">The name of the operation to invoke.</param>
        /// <param name="parameters">The operation parameters.</param>
        /// <returns>The result of the invoke operation.</returns>
        internal delegate object InvokeDelegate(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string name, object[] parameters);

        /// <summary>
        /// Delegate used to pass proxies a reference to the <see cref="DomainServiceProxyHelper.Submit"/> method.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="currentOriginalEntityMap">The mapping of current and original entities used with the utility <see cref="DomainServiceProxy.AssociateOriginal"/> method.</param>
        /// <param name="entity">The entity being submitted.</param>
        /// <param name="operationName">The name of the submit operation. For CUD operations, this can be null.</param>
        /// <param name="parameters">The submit operation parameters.</param>
        /// <param name="domainOperation">The type of submit operation.</param>
        internal delegate void SubmitDelegate(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, IDictionary<object, object> currentOriginalEntityMap, object entity, string operationName, object[] parameters, DomainOperation domainOperation);

        /// <summary>
        /// Used to maintain a mapping between requested contract types and generated proxy types.
        /// </summary>
        private static readonly IDictionary<Type, Type> proxyTypeMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Synchronization lock used when generating new proxy types.
        /// </summary>
        private static readonly object syncLock = new object();

        #endregion // Fields and Delegates

        #region Methods

        #region Proxy Factory Methods

        /// <summary>
        /// Creates a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.
        /// </summary>
        /// <typeparam name="TDomainServiceContract">The <see cref="DomainService"/> contract interface.</typeparam>
        /// <typeparam name="TDomainService">The <see cref="DomainService"/> type that implements <typeparamref name="TDomainServiceContract"/>.</typeparam>
        /// <returns>Returns a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.</returns>
        public static TDomainServiceContract Create<TDomainServiceContract, TDomainService>()
            where TDomainServiceContract : IDisposable
            where TDomainService : DomainService
        {
            // Create a HttpContextWrapper and use that as our context.
            HttpContextWrapper context = new HttpContextWrapper(HttpContext.Current);
            return DomainServiceProxy.Create<TDomainServiceContract, TDomainService>(context);
        }

        /// <summary>
        /// Creates a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.
        /// </summary>
        /// <typeparam name="TDomainServiceContract">The <see cref="DomainService"/> contract interface.</typeparam>
        /// <typeparam name="TDomainService">The <see cref="DomainService"/> type that implements <typeparamref name="TDomainServiceContract"/>.</typeparam>
        /// <param name="httpContext">The <see cref="HttpContextBase"/> instance that should be provided to <typeparamref name="TDomainService"/> 
        /// proxy instances.</param>
        /// <returns>Returns a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.</returns>
        public static TDomainServiceContract Create<TDomainServiceContract, TDomainService>(HttpContextBase httpContext)
            where TDomainServiceContract : IDisposable
            where TDomainService : DomainService
        {
            // Create a DomainServiceContext (with access to the provided HttpContextBase) and use that as our context
            HttpContextBaseServiceProvider serviceProvider = new HttpContextBaseServiceProvider(httpContext);
            DomainServiceContext context = new DomainServiceContext(serviceProvider, httpContext.User, DomainOperationType.Query);

            return DomainServiceProxy.Create<TDomainServiceContract, TDomainService>(context);
        }

        /// <summary>
        /// Creates a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.
        /// </summary>
        /// <typeparam name="TDomainServiceContract">The <see cref="DomainService"/> contract interface.</typeparam>
        /// <typeparam name="TDomainService">The <see cref="DomainService"/> type that implements <typeparamref name="TDomainServiceContract"/>.</typeparam>
        /// <param name="domainServiceContext">The <see cref="DomainServiceContext"/> instance that should be provided to <typeparamref name="TDomainService"/> 
        /// proxy instances.</param>
        /// <returns>Returns a <typeparamref name="TDomainService"/> proxy instance that implements 
        /// <typeparamref name="TDomainServiceContract"/>.</returns>
        public static TDomainServiceContract Create<TDomainServiceContract, TDomainService>(DomainServiceContext domainServiceContext)
            where TDomainServiceContract : IDisposable
            where TDomainService : DomainService
        {
            if (domainServiceContext == null)
            {
                throw new ArgumentNullException(nameof(domainServiceContext));
            }

            // Get or create the proxy type
            Type proxyType = DomainServiceProxy.GetProxyType<TDomainServiceContract, TDomainService>();

            // Instantiate and initialize it.  Here, we have intrinsic knowledge about our proxy instance
            // in that the proxy generator will implement IDomainServiceProxy as well.
            TDomainServiceContract proxyInstance = (TDomainServiceContract)Activator.CreateInstance(proxyType);
            MethodInfo initMethod = proxyType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
            initMethod.Invoke(proxyInstance, new object[] { typeof(TDomainService), domainServiceContext });

            return proxyInstance;
        }

        /// <summary>
        /// Gets a <typeparamref name="TDomainService"/> proxy <see cref="Type"/> that implements 
        /// <typeparamref name="TDomainServiceContract"/>.
        /// </summary>
        /// <typeparam name="TDomainServiceContract">The <see cref="DomainService"/> contract interface.</typeparam>
        /// <typeparam name="TDomainService">The <see cref="DomainService"/> type that implements <typeparamref name="TDomainServiceContract"/>.</typeparam>
        /// <returns>Returns a <typeparamref name="TDomainService"/> proxy <see cref="Type"/> that implements 
        /// <typeparamref name="TDomainServiceContract"/>.</returns>
        private static Type GetProxyType<TDomainServiceContract, TDomainService>()
            where TDomainService : DomainService
        {
            Type contract = typeof(TDomainServiceContract);
            Type[] contractGenericArguments = null;

            // If we're dealing with a generic type, get the underlying generic type definition
            // before retrieving (or creating) a proxy type.
            if (contract.IsGenericType)
            {
                contractGenericArguments = contract.GetGenericArguments();
                contract = contract.GetGenericTypeDefinition();
            }

            // Get or create the proxy type
            Type proxyType = DomainServiceProxy.GetOrGenerateProxyType(contract, typeof(TDomainService));

            // If we're dealing with a generic type, make the expected generic proxy type.
            if (contract.IsGenericType)
            {
                proxyType = proxyType.MakeGenericType(contractGenericArguments);
            }

            return proxyType;
        }

        /// <summary>
        /// Gets or creates a <see cref="DomainService"/> proxy <see cref="Type"/> for
        /// the provided <paramref name="contractType"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="contractType">The <see cref="DomainService"/> contract type.</param>
        /// <param name="domainServiceType">The <see cref="DomainService"/> type.</param>
        /// <returns>A generated proxy <see cref="Type"/>.</returns>
        private static Type GetOrGenerateProxyType(Type contractType, Type domainServiceType)
        {
            Debug.Assert(contractType != null, "The parameter 'contractType' is null.");
            Debug.Assert(contractType.IsInterface, "The Type 'contractType' is not an interface.");

            // Attempt to get the proxy type from our mapping
            Type proxyType;
            if (!DomainServiceProxy.proxyTypeMap.TryGetValue(contractType, out proxyType))
            {
                lock (syncLock)
                {
                    if (!DomainServiceProxy.proxyTypeMap.TryGetValue(contractType, out proxyType))
                    {
                        // Not found, so generate the type and cache it
                        proxyType = DomainServiceProxyGenerator.Generate(contractType, domainServiceType);
                        DomainServiceProxy.proxyTypeMap[contractType] = proxyType;

                        // Init the helper delegates
                        InitializeStaticDelegates(
                            proxyType,
                            new QueryHelperDelegate(DomainServiceProxyHelper.Query),
                            new InvokeDelegate(DomainServiceProxyHelper.Invoke),
                            new SubmitDelegate(DomainServiceProxyHelper.Submit));
                    }
                }
            }

            return proxyType;
        }

        /// <summary>
        /// Initializes a proxy's static delegate fields using the provided delegate values.
        /// </summary>
        /// <param name="proxyType">The proxy type.</param>
        /// <param name="queryDelegate">The query delegate.</param>
        /// <param name="invokeDelegate">The invoke delegate.</param>
        /// <param name="submitDelegate">The submit delegate.</param>
        private static void InitializeStaticDelegates(Type proxyType, Delegate queryDelegate, Delegate invokeDelegate, Delegate submitDelegate)
        {
            FieldInfo queryDelegateField = proxyType.GetField("queryDelegate", BindingFlags.Public | BindingFlags.Static);
            queryDelegateField.SetValue(null, queryDelegate);

            FieldInfo invokeDelegateField = proxyType.GetField("invokeDelegate", BindingFlags.Public | BindingFlags.Static);
            invokeDelegateField.SetValue(null, invokeDelegate);

            FieldInfo submitDelegateeField = proxyType.GetField("submitDelegate", BindingFlags.Public | BindingFlags.Static);
            submitDelegateeField.SetValue(null, submitDelegate);
        }

        #endregion Proxy Factory Methods

        #region AssociateOriginal Methods

        /// <summary>
        /// Associates a current entity with an original entity. Used to enable optimistic concurrency checks 
        /// on DomainService proxies.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="domainServiceProxy">The DomainService proxy.</param>
        /// <param name="current">The current entity.</param>
        /// <param name="original">The original entity.</param>
        public static void AssociateOriginal<TEntity>(object domainServiceProxy, TEntity current, TEntity original)
            where TEntity : new()
        {
            if (domainServiceProxy == null)
            {
                throw new ArgumentNullException(nameof(domainServiceProxy));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            PropertyInfo currentOriginalProp = domainServiceProxy.GetType().GetProperty("CurrentOriginalEntityMap", BindingFlags.Public | BindingFlags.Instance);

            if (currentOriginalProp == null)
            {
                throw new ArgumentException(Resource.DomainServiceProxy_InvalidProxyType, nameof(domainServiceProxy));
            }

            MethodInfo currentOriginalGetter = currentOriginalProp.GetGetMethod();

            IDictionary<object, object> currentOriginalEntityMap = (IDictionary<object, object>)currentOriginalGetter.Invoke(domainServiceProxy, null);
            currentOriginalEntityMap[current] = original;
        }

        #endregion // AssociateOriginal Methods

        #endregion // Methods

        #region Nested Types

        /// <summary>
        /// A <see cref="HttpContextBase"/> <see cref="IServiceProvider"/>.
        /// </summary>
        private sealed class HttpContextBaseServiceProvider : IServiceProvider
        {
            /// <summary>
            /// Gets the <see cref="HttpContextBase"/>.
            /// </summary>
            private readonly HttpContextBase _httpContextBase;

            /// <summary>
            /// Creates a new <see cref="HttpContextBaseServiceProvider"/> using the provided 
            /// <paramref name="httpContextBase"/>.
            /// </summary>
            /// <param name="httpContextBase">The <see cref="HttpContextBase"/>.</param>
            public HttpContextBaseServiceProvider(HttpContextBase httpContextBase)
            {
                if (httpContextBase == null)
                {
                    throw new ArgumentNullException(nameof(httpContextBase));
                }

                this._httpContextBase = httpContextBase;
            }

            /// <summary>
            /// Gets the service object of the specified type.
            /// </summary>
            /// <param name="serviceType">An object that specifies the type of service object to get.</param>
            /// <returns>A service object of type serviceType or null if there is no 
            /// service object of type serviceType.</returns>
            public object GetService(Type serviceType)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException(nameof(serviceType));
                }

                if (serviceType == typeof(IPrincipal))
                {
                    return this._httpContextBase.User;
                }

                if (serviceType == typeof(HttpContextBase))
                {
                    return this._httpContextBase;
                }

                return null;
            }
        }

        #endregion // Nested Types
    }
}
