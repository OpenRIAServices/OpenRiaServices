using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using OpenRiaServices;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.ServiceModel.Web;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Represents a JSON endpoint factory for <see cref="DomainService"/>s.
    /// </summary>
    public class JsonEndpointFactory : DomainServiceEndpointFactory
    {
        private const string TransmitMetadataConfigEntry = "transmitMetadata";

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEndpointFactory"/> class.
        /// </summary>
        public JsonEndpointFactory()
        {
        }

        /// <summary>
        /// Creates an endpoint based on the specified address.
        /// </summary>
        /// <param name="contract">The endpoint's contract.</param>
        /// <param name="address">The endpoint's base address.</param>
        /// <returns>An endpoint.</returns>
        protected override ServiceEndpoint CreateEndpointForAddress(ContractDescription contract, Uri address)
        {
            WebHttpBinding binding = new WebHttpBinding();
            binding.MaxReceivedMessageSize = ServiceUtility.MaxReceivedMessageSize;
            ServiceUtility.SetReaderQuotas(binding.ReaderQuotas);

            if (address.Scheme.Equals(Uri.UriSchemeHttps))
            {
                binding.Security.Mode = WebHttpSecurityMode.Transport;
            }
            else if (ServiceUtility.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
            }

            if (ServiceUtility.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Transport.ClientCredentialType = ServiceUtility.CredentialType;
            }

            ServiceEndpoint endpoint = new ServiceEndpoint(contract, binding, new EndpointAddress(address.OriginalString + "/" + this.Name));
            endpoint.Behaviors.Add(new DomainServiceWebHttpBehavior()
            {
                DefaultBodyStyle = WebMessageBodyStyle.Wrapped,
                DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                DefaultOutgoingResponseFormat = WebMessageFormat.Json
            });

            if (this.Parameters[JsonEndpointFactory.TransmitMetadataConfigEntry] != null)
            {
                bool transmitMetadata = false;
                if (bool.TryParse(this.Parameters[JsonEndpointFactory.TransmitMetadataConfigEntry], out transmitMetadata) && transmitMetadata)
                {
                    endpoint.Behaviors.Add(new ServiceMetadataEndpointBehavior());
                }
            }

            return endpoint;
        }
    }
}
