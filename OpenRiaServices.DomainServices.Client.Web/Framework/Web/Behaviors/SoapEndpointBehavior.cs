using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.DomainServices.Client.Web.Behaviors
{
    /// <summary>
    /// A SOAP endpoint behavior which injects a message inspector that adds query headers.
    /// </summary>
    sealed partial class SoapEndpointBehavior : IEndpointBehavior
    {
        /// <summary>
        /// Message insepctor to use if it is set to a non-<c>null</c> value.
        /// </summary>
        private readonly IClientMessageInspector _cookieInspector;
        private readonly QueryInspector _soapQueryInspector;

        public SoapEndpointBehavior(WcfDomainClientFactory factory)
        {
            _soapQueryInspector = new QueryInspector(factory);
            _cookieInspector = factory.SharedCookieMessageInspector;
        }

        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param>
        /// <param name="clientRuntime">The client runtime to be customized.</param>
        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
#if SILVERLIGHT
            var inspectors = clientRuntime.MessageInspectors;
#else
            var inspectors = clientRuntime.ClientMessageInspectors;
#endif

            inspectors.Add(_soapQueryInspector);
            if (_cookieInspector != null)
                inspectors.Add(_cookieInspector);
        }

        /// <summary>
        /// Implements a modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Implement to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
            foreach (var od in endpoint.Contract.Operations)
            {
                // Add FaultDescription if [FaultContractAttribute] has not already been added (by old code-generation)
                if (!od.Faults.Any(f => f.DetailType == typeof(DomainServiceFault)))
                {
                    od.Faults.Add(new FaultDescription(od.Messages[0].Action + "DomainServiceFault")
                    {
                        DetailType = typeof(DomainServiceFault),
                        Name = typeof(DomainServiceFault).Name,
                        Namespace = "DomainServices",
                    });
                }
            }
        }
    }
}
