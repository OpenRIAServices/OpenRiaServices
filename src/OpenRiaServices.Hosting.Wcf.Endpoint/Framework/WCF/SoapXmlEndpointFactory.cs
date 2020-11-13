using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf
{
    /// <summary>
    /// Represents a SOAP w/ XML encoding endpoint factory for <see cref="DomainService"/>s.
    /// </summary>
    public class SoapXmlEndpointFactory : DomainServiceEndpointFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoapXmlEndpointFactory"/> class.
        /// </summary>
        public SoapXmlEndpointFactory()
            : base("soap")
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
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = ServiceUtility.MaxReceivedMessageSize;
            ServiceUtility.SetReaderQuotas(binding.ReaderQuotas);

            if (address.Scheme.Equals(Uri.UriSchemeHttps))
            {
                binding.Security.Mode = BasicHttpSecurityMode.Transport;
            }
            else if (ServiceUtility.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            }

            if (ServiceUtility.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Transport.ClientCredentialType = ServiceUtility.CredentialType;
            }

            // Enable metadata generation
            if (contract.Behaviors.Find<ServiceMetadataContractBehavior>() is var metadataBehaviour)
                metadataBehaviour.MetadataGenerationDisabled = false;

            ServiceEndpoint endpoint = new ServiceEndpoint(contract, binding, new EndpointAddress(address.OriginalString + "/" + this.Name));
            endpoint.Behaviors.Add(new SoapQueryBehavior());
            return endpoint;
        }
    }
}
