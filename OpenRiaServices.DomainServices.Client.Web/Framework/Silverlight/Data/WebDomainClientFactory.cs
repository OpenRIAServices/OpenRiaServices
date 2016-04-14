using System;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.DomainServices.Client.Web
{
    /// <summary>
    /// Creates <see cref="WebDomainClient{TContract}"/> instances
    /// </summary>
    public class WebDomainClientFactory : DomainClientFactory
    {
        private readonly MethodInfo _createInstanceMethod;
        private CookieContainer _cookieContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClientFactory" /> class.
        /// </summary>
        public WebDomainClientFactory()
        {
            _createInstanceMethod = typeof(WebDomainClientFactory).GetMethod("CreateInstance", BindingFlags.NonPublic | BindingFlags.Instance);

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
        /// Cookie container to shared by all created <see cref="DomainClient"/>s.
        /// If value is <c>null</c> then cookies will not be managed.
        /// This is required when using Forms Authentication (except for Silverlight) and is 
        /// therefore enabled by default.
        /// </summary>
        public CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set
            {
                _cookieContainer = value;
                SharedCookieMessageInspector = (value != null) ? new SharedCookieMessageInspector(value) : null;
            }
        }

        /// <summary>
        /// When <see cref="CookieContainer"/> is set to a non-<c>null</c> value then
        /// this inspector is used by 
        /// <see cref="WebDomainClientWebHttpBehavior.ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint, ClientRuntime)"/>
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
    ///  either a IChannelInitializer (not availible in Silverlight)
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
