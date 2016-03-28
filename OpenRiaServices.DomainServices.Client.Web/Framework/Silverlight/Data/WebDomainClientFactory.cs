using System;
using System.Net;
using System.Reflection;

namespace OpenRiaServices.DomainServices.Client.Web
{
    /// <summary>
    /// Creates <see cref="WebDomainClient{TContract}"/> instances
    /// </summary>
    public class WebDomainClientFactory : DomainClientFactory
    {
        readonly MethodInfo _createInstanceMethod;

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
        public CookieContainer CookieContainer { get; set; }
    }
}
