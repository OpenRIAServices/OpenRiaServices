using OpenRiaServices.Hosting.WCF;
using System;
using System.ServiceModel;

namespace Cities
{
    public class CityDomainServiceHostFactory : DomainServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new CityDomainServiceHost(serviceType, baseAddresses);
        }
    }
}
