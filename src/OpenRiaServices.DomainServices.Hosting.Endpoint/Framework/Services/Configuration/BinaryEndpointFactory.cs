using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using OpenRiaServices;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.ServiceModel.Channels;
using System.Net;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Represents a SOAP w/ XML encoding endpoint factory for <see cref="DomainService"/>s.
    /// </summary>
    public class BinaryEndpointFactory : DomainServiceEndpointFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoapXmlEndpointFactory"/> class.
        /// </summary>
        public BinaryEndpointFactory()
        {
        }

        /// <summary>
        /// Creates endpoints based on the specified description.
        /// </summary>
        /// <param name="description">The <see cref="DomainServiceDescription"/> of the <see cref="DomainService"/> to create the endpoints for.</param>
        /// <param name="serviceHost">The service host for which the endpoints will be created.</param>
        /// <returns>The endpoints that were created.</returns>
        public override IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost)
        {
            ContractDescription contract = this.CreateContract(description);
            List<ServiceEndpoint> endpoints = new List<ServiceEndpoint>();
            foreach (Uri address in serviceHost.BaseAddresses)
            {
                endpoints.Add(this.CreateEndpointForAddress(contract, address));
            }
            return endpoints;
        }

        /// <summary>
        /// Creates an endpoint based on the specified address.
        /// </summary>
        /// <param name="contract">The endpoint's contract.</param>
        /// <param name="address">The endpoint's base address.</param>
        /// <returns>An endpoint.</returns>
        private ServiceEndpoint CreateEndpointForAddress(ContractDescription contract, Uri address)
        {
            Binding binding = CreateBinding(address);

            ServiceEndpoint endpoint = new ServiceEndpoint(contract, binding, new EndpointAddress(address.OriginalString + "/" + this.Name));
            endpoint.Behaviors.Add(new SoapQueryBehavior());
            return endpoint;
        }

        private static Binding CreateBinding(Uri address)
        {
            var encoding = new BinaryMessageEncodingBindingElement()
            {
                 
            };
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
            //transport.ManualAddressing = true;
            transport.MaxReceivedMessageSize = ServiceUtility.MaxReceivedMessageSize;

            if (ServiceUtility.AuthenticationScheme != AuthenticationSchemes.None)
            {
                transport.AuthenticationScheme = ServiceUtility.AuthenticationScheme;
            }

            
            return new CustomBinding(encoding, transport);
        }

        /// <summary>
        /// Creates a contract from the specified description.
        /// </summary>
        /// <param name="description">The description to create a contract from.</param>
        /// <returns>A <see cref="ContractDescription"/>.</returns>
        private ContractDescription CreateContract(DomainServiceDescription description)
        {
            Type domainServiceType = description.DomainServiceType;

            // PERF: We should consider just looking at [ServiceDescription] directly.
            ServiceDescription serviceDesc = ServiceDescription.GetService(domainServiceType);

            // Use names from [ServiceContract], if specified.
            ServiceContractAttribute sca = TypeDescriptor.GetAttributes(domainServiceType)[typeof(ServiceContractAttribute)] as ServiceContractAttribute;
            if (sca != null)
            {
                if (!String.IsNullOrEmpty(sca.Name))
                {
                    serviceDesc.Name = sca.Name;
                }
                if (!String.IsNullOrEmpty(sca.Namespace))
                {
                    serviceDesc.Name = sca.Namespace;
                }
            }

            ContractDescription contractDesc = new ContractDescription(serviceDesc.Name + this.Name, serviceDesc.Namespace)
            {
                ConfigurationName = serviceDesc.ConfigurationName + this.Name,
                ContractType = domainServiceType
            };

            // Add domain service behavior which takes care of instantiating DomainServices.
            ServiceUtility.EnsureBehavior<DomainServiceBehavior>(contractDesc);

            // Load the ContractDescription from the DomainServiceDescription.
            ServiceUtility.LoadContractDescription(contractDesc, description);

            return contractDesc;
        }
    }
}
