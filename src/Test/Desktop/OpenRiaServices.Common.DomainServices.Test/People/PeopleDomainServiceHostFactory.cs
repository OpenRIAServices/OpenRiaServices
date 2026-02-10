using System;
using System.ServiceModel;
using OpenRiaServices.Hosting.Wcf;

namespace People
{
    public class PeopleDomainServiceHostFactory : DomainServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new PeopleDomainServiceHost(serviceType, baseAddresses);
        }
    }
}
