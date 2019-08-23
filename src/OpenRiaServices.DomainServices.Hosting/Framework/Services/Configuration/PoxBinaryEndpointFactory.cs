﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            contract.Behaviors.Add(new ServiceMetadataContractBehavior() { MetadataGenerationDisabled = true });
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
            endpoint.Behaviors.Add(new SilverlightFaultBehavior());
            return endpoint;
        }

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
                    serviceDesc.Namespace = sca.Namespace;
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
