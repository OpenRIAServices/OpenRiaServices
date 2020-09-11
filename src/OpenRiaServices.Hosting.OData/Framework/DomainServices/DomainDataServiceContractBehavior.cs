using System;
using System.ServiceModel;

namespace OpenRiaServices.Hosting.OData
{
    #region Namespaces
    
    using System.Diagnostics;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using OpenRiaServices.Server;
    
    #endregion

    /// <summary>
    /// Applies the instantiation behavior for the DomainService per request on the 
    /// domain data service endpoint.
    /// </summary>
    internal class DomainDataServiceContractBehavior : IContractBehavior
    {
        /// <summary>Configures any binding elements to support the contract behavior.</summary>
        /// <param name="contractDescription">The contract description to modify.</param>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        /// <summary>Implements a modification or extension of the client across a contract.</summary>
        /// <param name="contractDescription">The contract description for which the extension is intended.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="clientRuntime">The client runtime.</param>
        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        /// <summary>Implements a modification or extension of the service across a contract.</summary>
        /// <param name="contractDescription">The contract description to be modified.</param>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="dispatchRuntime">The dispatch runtime that controls service execution.</param>
        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            dispatchRuntime.InstanceProvider = new DomainDataServiceInstanceProvider();
        }

        /// <summary>Implement to confirm that the contract and endpoint can support the contract behavior.</summary>
        /// <param name="contractDescription">The contract to validate.</param>
        /// <param name="endpoint">The endpoint to validate.</param>
        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }

        // Used to carry information between the IInstanceProvider and DomainDataServiceOperationInvoker 
        // (where we actually instantiate the DomainService).
        internal class DomainDataServiceInstanceInfo
        {
            public Type DomainServiceType
            {
                get;
                set;
            }

            public DomainService DomainServiceInstance
            {
                get;
                set;
            }
        }

        /// <summary>Service object instantiator.</summary>
        private class DomainDataServiceInstanceProvider : IInstanceProvider
        {
            /// <summary>
            /// Returns a service object given the specified InstanceContext object.
            /// </summary>
            /// <param name="instanceContext">The current InstanceContext object.</param>
            /// <returns>A user-defined service object.</returns>
            public object GetInstance(InstanceContext instanceContext)
            {
                Debug.Assert(instanceContext != null, "instanceContext should not be null.");
                return this.GetInstance(instanceContext, null);
            }

            /// <summary>
            /// Returns a service object given the specified InstanceContext object.
            /// </summary>
            /// <param name="instanceContext">The current InstanceContext object.</param>
            /// <param name="message">The message that triggered the creation of a service object.</param>
            /// <returns>The service object.</returns>
            public object GetInstance(InstanceContext instanceContext, Message message)
            {
                Debug.Assert(instanceContext != null, "instanceContext should not be null.");

                DomainDataServiceInstanceInfo instanceInfo = new DomainDataServiceInstanceInfo();
                instanceInfo.DomainServiceType = instanceContext.Host.Description.ServiceType;

                // Since we require more contextual information at the point in time when a 
                // DomainService is created, we return an info object and do delay instantiation 
                // (the invoker will call back into us later to do instantiation).
                return instanceInfo;
            }

            /// <summary>
            /// Called when an InstanceContext object recycles a service object.
            /// </summary>
            /// <param name="instanceContext">The service's instance context.</param>
            /// <param name="instance">The service object to be recycled.</param>
            public void ReleaseInstance(InstanceContext instanceContext, object instance)
            {
                DomainDataServiceInstanceInfo instanceInfo = (DomainDataServiceInstanceInfo)instance;
                if (instanceInfo.DomainServiceInstance != null)
                {
                    DomainService.Factory.ReleaseDomainService(instanceInfo.DomainServiceInstance);
                }
            }
        }
    }
}