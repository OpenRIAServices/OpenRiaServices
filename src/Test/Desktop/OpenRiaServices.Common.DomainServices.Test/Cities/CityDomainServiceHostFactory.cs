using System;
using System.ServiceModel;
using OpenRiaServices;
using OpenRiaServices.Hosting;

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
