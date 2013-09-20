using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using Resx = Microsoft.ServiceModel.DomainServices.Hosting.Resource;

namespace Microsoft.ServiceModel.DomainServices.Hosting
{
    /// <summary>
    /// Represents a tracing endpoint factory for a <see cref="System.ServiceModel.DomainServices.Hosting.DomainServiceHost"/>. Adding
    /// this endpoint factory to the domain service host results in exposing traces of all WCF services running in the application domain over 
    /// a WCF REST endpoint in the ATOM, XML, or HTML format. In order to enable this functionality, in addition to adding this endpoint factory to the 
    /// <see cref="System.ServiceModel.DomainServices.Hosting.DomainServiceHost"/>, one must register the <see cref="InMemoryTraceListener"/> for the 
    /// System.ServiceModel traces through the system.diagnostics section in the configuration file.
    /// </summary>
    public class TracingDomainServiceEndpointFactory : DomainServiceEndpointFactory
    {
        /// <summary>
        /// Creates an instance of the class. 
        /// </summary>
        public TracingDomainServiceEndpointFactory() : base() { }

        /// <summary>
        /// Creates a set of WCF REST service endpoints in the <see cref="System.ServiceModel.DomainServices.Hosting.DomainServiceHost"/> which 
        /// expose traces of WCF services in the ATOM, XML, or HTML format. One WCF REST endpoint is added for each HTTP or HTTPS base address from the specified serviceHost.
        /// The address of the endpoint is obtained by appending the name of the TracingDomainServiceEndpointFactory as specified in the domainServices section of the configuration file
        /// to the base address. Furthermore, the UriTemplate of each of the endpoints is specified by the <see cref="WcfTraceService"/> service contract and allows for selection of the 
        /// response contract between ATOM, XML, or HTML. 
        /// </summary>
        /// <param name="description">WCF RIA service description.</param>
        /// <param name="serviceHost">Service host to which endpoints will be added.</param>
        /// <returns>The collection of endpoints.</returns>
        public override IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost)
        {
            if (serviceHost == null)
            {
                throw new ArgumentNullException("serviceHost");
            }

            if (this.Parameters["maxEntries"] != null)
            {
                int maxEntries;
                if (int.TryParse(this.Parameters["maxEntries"], out maxEntries))
                {
                    InMemoryTraceListener.MaxEntries = maxEntries;
                }
                else
                {
                    throw new InvalidOperationException(Resx.MaxEntriesAttributeMustBeAPositiveInteger);
                }
            }

            ContractDescription contract = ContractDescription.GetContract(typeof(WcfTraceService));
            contract.Behaviors.Add(new ServiceMetadataContractBehavior { MetadataGenerationDisabled = true });
            
            List<ServiceEndpoint> tracingEndpoints = new List<ServiceEndpoint>();
            foreach (Uri baseAddress in serviceHost.BaseAddresses)
            {
                WebHttpBinding binding = new WebHttpBinding();
                if (baseAddress.Scheme.Equals(Uri.UriSchemeHttps))
                {
                    binding.Security.Mode = WebHttpSecurityMode.Transport;
                }
                else if (!baseAddress.Scheme.Equals(Uri.UriSchemeHttp))
                {
                    continue;
                }

                ServiceEndpoint endpoint = new ServiceEndpoint(contract, binding, new EndpointAddress(baseAddress.OriginalString + "/" + this.Name));
                endpoint.Behaviors.Add(new WebHttpBehavior());
                endpoint.Behaviors.Add(new TracingEndpointBehavior { ServiceHost = serviceHost });
                
                tracingEndpoints.Add(endpoint);
            }
            
            return tracingEndpoints;
        }

        class TracingEndpointBehavior : IEndpointBehavior, IInstanceContextProvider, IInstanceProvider
        {
            public ServiceHostBase ServiceHost { get; set; }

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                if (endpointDispatcher == null)
                {
                    throw new ArgumentNullException("endpointDispatcher");
                }

                endpointDispatcher.DispatchRuntime.SynchronizationContext = null;
                endpointDispatcher.DispatchRuntime.ConcurrencyMode = ConcurrencyMode.Single;
                endpointDispatcher.DispatchRuntime.InstanceContextProvider = this;
                endpointDispatcher.DispatchRuntime.InstanceProvider = this;
                endpointDispatcher.DispatchRuntime.SingletonInstanceContext = new InstanceContext(this.ServiceHost, WcfTraceService.Instance);
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }

            public InstanceContext GetExistingInstanceContext(Message message, IContextChannel channel)
            {
                return null;
            }

            public void InitializeInstanceContext(InstanceContext instanceContext, Message message, IContextChannel channel)
            {
            }

            public bool IsIdle(InstanceContext instanceContext)
            {
                return true;
            }

            public void NotifyIdle(InstanceContextIdleCallback callback, InstanceContext instanceContext)
            {
            }

            public object GetInstance(InstanceContext instanceContext, Message message)
            {
                return WcfTraceService.Instance;
            }

            public object GetInstance(InstanceContext instanceContext)
            {
                return WcfTraceService.Instance;
            }

            public void ReleaseInstance(InstanceContext instanceContext, object instance)
            {
            }
        }
    }
}
