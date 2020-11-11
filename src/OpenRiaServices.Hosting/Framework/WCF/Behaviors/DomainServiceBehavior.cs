using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.WCF.Behaviors
{
    internal class DomainServiceBehavior : IContractBehavior
    {
        public DomainServiceBehavior()
        {
        }

        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            dispatchRuntime.InstanceProvider = new DomainServiceInstanceProvider();
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }

        internal class DomainServiceInstanceProvider : IInstanceProvider
        {
            public object GetInstance(InstanceContext instanceContext)
            {
                Debug.Assert(instanceContext != null, "instanceContext should not be null.");

                return this.GetInstance(instanceContext, null);
            }

            public object GetInstance(InstanceContext instanceContext, Message message)
            {
                Debug.Assert(instanceContext != null, "instanceContext should not be null.");

                DomainServiceInstanceInfo instanceInfo = new DomainServiceInstanceInfo();
                instanceInfo.DomainServiceType = instanceContext.Host.Description.ServiceType;

                // since we require more contextual information at the point in
                // time when a DomainService is created, we return an info object and
                // do delay instantiation (the invoker will call back into us
                // later to do instantiation)
                return instanceInfo;
            }

            public void ReleaseInstance(InstanceContext instanceContext, object instance)
            {
                DomainServiceInstanceInfo instanceInfo = (DomainServiceInstanceInfo)instance;
                if (instanceInfo.DomainServiceInstance != null)
                {
                    DomainService.Factory.ReleaseDomainService(instanceInfo.DomainServiceInstance);
                }
            }
        }

        // Used to carry information between the IInstanceProvider and DomainOperationInvoker (where we actually 
        // instantiate the DomainService).
        internal class DomainServiceInstanceInfo
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
    }
}
