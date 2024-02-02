using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using OpenRiaServices.Client.Web.Behaviors;

namespace OpenRiaServices.Client.Web
{
    /// <summary>
    /// Creates <see cref="WebDomainClient{TContract}"/> instances
    /// For connecting to services using the default REST with binary encoding protocol.
    /// </summary>
    public class WebDomainClientFactory : WcfDomainClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClientFactory" /> class.
        /// </summary>
        [Obsolete("The WCF based WebDomainClientFactory will not receive any new changes. It is recommeded to switch to OpenRiaServices.Client.DomainClients.BinaryHttpDomainClientFactory instead.")]
        public WebDomainClientFactory()
            : base("binary")
        {
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
        protected override ChannelFactory<TContract> CreateChannelFactory<TContract>(Uri endpoint, bool requiresSecureEndpoint)
        {
            ChannelFactory<TContract> factory = null;

            try
            {
                factory = base.CreateChannelFactory<TContract>(endpoint, requiresSecureEndpoint);
                factory.Endpoint.Behaviors.Add(new WebDomainClientWebHttpBehavior()
                {
                    DefaultBodyStyle = System.ServiceModel.Web.WebMessageBodyStyle.Wrapped,
                    MessageInspector = SharedCookieMessageInspector,
                });
            }
            catch
            {
                ((IDisposable)factory)?.Dispose();
                throw;
            }

            return factory;
        }

        /// <summary>
        /// Setup the default WCF <see cref="Binding" /> for the server communication.
        /// Using "REST" w/ binary encoding
        /// </summary>
        /// <param name="endpoint">Absolute service URI without protocol suffix such as "/binary"</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if communication must be secured, otherwise <c>false</c></param>
        /// <returns>A <see cref="CustomBinding"/> using REST and binary encoding</returns>
        protected override Binding CreateBinding(Uri endpoint, bool requiresSecureEndpoint)
        {
            // By default, use "REST" w/ binary encoding.
            PoxBinaryMessageEncodingBindingElement encoder = new PoxBinaryMessageEncodingBindingElement();

            HttpTransportBindingElement transport;
            if (endpoint.Scheme == Uri.UriSchemeHttps)
            {
                transport = new HttpsTransportBindingElement();
            }
            else
            {
                transport = new HttpTransportBindingElement();
            }
            transport.ManualAddressing = true;
            transport.MaxReceivedMessageSize = int.MaxValue;

            List<BindingElement> bindingElements = new List<BindingElement>() { encoder, transport };
            if (CookieContainer != null)
            {
#if SILVERLIGHT
                bindingElements.Insert(0, new HttpCookieContainerBindingElement());
#else
                transport.AllowCookies = true;
#endif
            }

            return new CustomBinding(bindingElements);
        }
    }
}
