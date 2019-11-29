using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Represents a REST w/ binary encoding endpoint factory for <see cref="DomainService"/>s.
    /// </summary>
    public class PoxBinaryEndpointFactory : DomainServiceEndpointFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoxBinaryEndpointFactory"/> class.
        /// </summary>
        public PoxBinaryEndpointFactory()
        {
            this.Name = "binary";
        }

        /// <summary>
        /// Creates an endpoint based on the specified address.
        /// </summary>
        /// <param name="contract">The endpoint's contract.</param>
        /// <param name="address">The endpoint's base address.</param>
        /// <returns>An endpoint.</returns>
        protected override ServiceEndpoint CreateEndpointForAddress(ContractDescription contract, Uri address)
        {
            PoxBinaryMessageEncodingBindingElement encoding = new PoxBinaryMessageEncodingBindingElement();
            ServiceUtility.SetReaderQuotas(encoding.ReaderQuotas);

            HttpTransportBindingElement transport;
            if (address.Scheme.Equals(Uri.UriSchemeHttps))
            {
                transport = new HttpsTransportBindingElement();
            }
            else
            {
                transport = new HttpTransportBindingElement();
            }
            transport.ManualAddressing = true;
            transport.MaxReceivedMessageSize = ServiceUtility.MaxReceivedMessageSize;

            if (ServiceUtility.AuthenticationScheme != AuthenticationSchemes.None)
            {
                transport.AuthenticationScheme = ServiceUtility.AuthenticationScheme;
            }

            CustomBinding binding =
                new CustomBinding(
                    new BindingElement[]
                    {
                        encoding,
                        transport
                    });

            ServiceEndpoint endpoint = new ServiceEndpoint(contract, binding, new EndpointAddress(address.OriginalString + "/" + this.Name));
            endpoint.Behaviors.Add(new DomainServiceWebHttpBehavior()
            {
                DefaultBodyStyle = System.ServiceModel.Web.WebMessageBodyStyle.Wrapped
            });
            return endpoint;
        }
    }
}
