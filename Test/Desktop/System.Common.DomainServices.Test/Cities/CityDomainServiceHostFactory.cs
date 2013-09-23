using System;
using OpenRiaServices;
using OpenRiaServices.DomainServices.Hosting;

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
