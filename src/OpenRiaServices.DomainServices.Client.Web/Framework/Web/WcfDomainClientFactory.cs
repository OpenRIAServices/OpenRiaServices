using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace OpenRiaServices.DomainServices.Client.Web
{
#if NETSTANDARD
    /// <summary>
    /// Base class DomainClientFactories targeting WCF and creating <see cref="WebDomainClient{TContract}"/> instances.
    /// For most uses you should use a concerete implementation such as 
    /// "WebDomainClientFactory" or <see cref="SoapDomainClientFactory"/>
    /// instead.
    /// </summary>
#else
    /// <summary>
    /// Base class DomainClientFactories targeting WCF and creating <see cref="WebDomainClient{TContract}"/> instances.
    /// For most uses you should use a concerete implementation such as 
    /// <see cref="WebDomainClientFactory"/> or <see cref="SoapDomainClientFactory"/>
    /// instead.
    /// </summary>
#endif
    public abstract class WcfDomainClientFactory : DomainClientFactory
    {
        private readonly MethodInfo _createInstanceMethod;
        private readonly Dictionary<Type, ChannelFactory> _channelFactoryCache = new Dictionary<Type, ChannelFactory>();
        private readonly object _channelFactoryCacheLock = new object();
        private CookieContainer _cookieContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfDomainClientFactory" /> class.
        /// </summary>
        protected WcfDomainClientFactory()
        {
            _createInstanceMethod = typeof(WcfDomainClientFactory).GetMethod(nameof(CreateInstance), BindingFlags.NonPublic | BindingFlags.Instance);

            // Silverlight uses the browser's cookies by default, in which case we should not manage cookies manually
#if SILVERLIGHT
            CookieContainer = null;
#else
            CookieContainer = new CookieContainer();
#endif
        }

        /// <summary>
        /// Creates an <see cref="WebDomainClient{TContract}"/> instance.
        /// </summary>
        /// <param name="serviceContract">The service contract (not null).</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if DomainService has the RequiresSecureEndpoint attribute and encryption should be enabled.</param>
        /// <returns>
        /// A <see cref="DomainClient" /> to use when communicating with the service
        /// </returns>
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            var actualMethod = _createInstanceMethod.MakeGenericMethod(serviceContract);
            var parameters = new object[] { serviceUri, requiresSecureEndpoint };

            return (DomainClient)actualMethod.Invoke(this, parameters);
        }

        /// <summary>
        /// Creates actual WebDomainClient instance.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract.</typeparam>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="requiresSecureEndpoint">if set to <c>true</c> [requires secure endpoint].</param>
        /// <returns></returns>
        private WebDomainClient<TContract> CreateInstance<TContract>(Uri serviceUri, bool requiresSecureEndpoint)
             where TContract : class
        {
            return new WebDomainClient<TContract>(serviceUri, requiresSecureEndpoint, this);
        }

        /// <summary>
        /// Cookie container to be shared by all created <see cref="DomainClient"/>s.
        /// If value is <c>null</c> then cookies will not be managed.
        /// This is required when using cookie based Authentication (except for Silverlight) and is 
        /// therefore enabled by default.
        /// </summary>
        public CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set
            {
                _cookieContainer = value;

                // Chainging the CookieContainer means that we need a new MessageInspector so we can no longer reuse
                // the existing channels for new DomainClients
                lock (_channelFactoryCacheLock)
                {
                    SharedCookieMessageInspector = (value != null) ? new SharedCookieMessageInspector(value) : null;
                    _channelFactoryCache.Clear();
                }
            }
        }

        /// <summary>
        /// Creates a channel factory for use by a DomainClient to communicate with the server.
        /// </summary>
        /// <remarks>
        ///  This is not used if a ChannelFactory was passed to the ctor of the <see cref="WebDomainClient{TContract}"/>
        /// </remarks>
        /// <param name="endpoint">Absolute service URI without protocol suffix such as "/binary"</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if communication must be secured, otherwise <c>false</c></param>
        /// <param name="domainClient">the domainclient which request the channel factory</param>
        /// <returns>The channel used to communicate with the server.</returns>
        internal ChannelFactory<TContract> CreateChannelFactory<TContract>(Uri endpoint, bool requiresSecureEndpoint, WebDomainClient<TContract> domainClient)
            where TContract : class
        {
            ChannelFactory<TContract> channelFactory;

            lock (_channelFactoryCacheLock)
            {
                if (_channelFactoryCache.TryGetValue(typeof(TContract), out var existingFactory)
                    // This should never happen, but check anyway just to be safe
                    && existingFactory.State != CommunicationState.Faulted)
                {
                    channelFactory = (ChannelFactory<TContract>)existingFactory;
                }
                else
                {
                    // Create and initialize a new channel factory
                    channelFactory = CreateChannelFactory<TContract>(endpoint, requiresSecureEndpoint);
                    InitializeChannelFactory(domainClient, channelFactory);

                    _channelFactoryCache[typeof(TContract)] = channelFactory;
                }
            }

            return channelFactory;
        }

        /// <summary>
        /// Performs one time initialization of the ChannelFactory
        /// </summary>
        private static void InitializeChannelFactory<TContract>(WebDomainClient<TContract> domainClient, ChannelFactory<TContract> channelFactory) where TContract : class
        {
            var originalSyncContext = SynchronizationContext.Current;
            try
            {
                foreach (OperationDescription op in channelFactory.Endpoint.Contract.Operations)
                {
                    foreach (Type knownType in domainClient.KnownTypes)
                    {
                        op.KnownTypes.Add(knownType);
                    }
                }

                SynchronizationContext.SetSynchronizationContext(null);
                channelFactory.Open();
            }
            catch
            {
                ((IDisposable)channelFactory)?.Dispose();
                throw;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalSyncContext);
            }
        }


        /// <summary>
        /// Creates a channel factory for use by a DomainClient to communicate with the server.
        /// </summary>
        /// <remarks>
        ///  This is not used if a ChannelFactory was passed to the ctor of the <see cref="WebDomainClient{TContract}"/>
        /// </remarks>
        /// <param name="endpoint">Absolute service URI without protocol suffix such as "/binary"</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if communication must be secured, otherwise <c>false</c></param>
        /// <returns>The channel used to communicate with the server.</returns>
        protected virtual ChannelFactory<TContract> CreateChannelFactory<TContract>(Uri endpoint, bool requiresSecureEndpoint)
        {
            ChannelFactory<TContract> factory = null;

            try
            {
                var binding = CreateBinding(endpoint, requiresSecureEndpoint);
                var address = CreateEndpointAddress(endpoint, requiresSecureEndpoint);
                factory = new ChannelFactory<TContract>(binding, address);

#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    // in debug mode set the timeout to a higher number to
                    // facilitate debugging
                    factory.Endpoint.Binding.OpenTimeout = TimeSpan.FromMinutes(5);
                }
#endif
            }
            catch
            {
                ((IDisposable)factory)?.Dispose();
                throw;
            }

            return factory;
        }

        /// <summary>
        /// Get an <see cref="EndpointAddress" /> that identifies the server endpoint
        /// </summary>
        /// <param name="endpoint">Absolute service URI without protocol suffix such as "/binary"</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if communication must be secured, otherwise <c>false</c></param>
        /// <returns><see cref="EndpointAddress"/> where target uri has the protocol suffix ("/binary") set </returns>
        protected abstract EndpointAddress CreateEndpointAddress(Uri endpoint, bool requiresSecureEndpoint);

        /// <summary>
        /// Setup the default WCF <see cref="Binding" /> for the server communication.
        /// Using "REST" w/ binary encoding
        /// </summary>
        /// <param name="endpoint">Absolute service URI without protocol suffix such as "/binary"</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if communication must be secured, otherwise <c>false</c></param>
        /// <returns>A <see cref="CustomBinding"/> using REST and binary encoding</returns>
        protected abstract Binding CreateBinding(Uri endpoint, bool requiresSecureEndpoint);

        /// <summary>
        /// When <see cref="CookieContainer"/> is set to a non-<c>null</c> value then
        /// this inspector is used by wcf based transports to setup cookie suport
        /// </summary>
        internal IClientMessageInspector SharedCookieMessageInspector { get; private set; }
    }

    /// <summary>
    /// Helper class which ensures that we use the CookieContainer for each request
    /// based upon Kyle McClellan's blog post
    /// https://blogs.msdn.microsoft.com/kylemc/2010/05/14/ria-services-authentication-out-of-browser/
    /// 
    /// </summary>
    /// <remarks>
    /// We should try to make this a one time initialization instead
    /// using 
    ///  either a IChannelInitializer (not available in Silverlight)
    /// or 
    ///  by initializing the ChannelFactory by "opening" it so we can use 
    ///  channelFactory.GetProperty{IHttpCookieContainerManager}().CookieContainer = ..
    ///  in CreateChannelFactory function. (which requires some refactoring if we
    ///  are still to allow customization of the ChannelFactory).
    /// </remarks>
    class SharedCookieMessageInspector : IClientMessageInspector
    {
        public CookieContainer _cookieContainer { get; set; }

        public SharedCookieMessageInspector(CookieContainer cookieContainer)
        {
            _cookieContainer = cookieContainer;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // do nothing
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // make sure the channel uses the shared cookie container
            channel.GetProperty<IHttpCookieContainerManager>().CookieContainer =
                this._cookieContainer;
            return null;
        }
    }
}
